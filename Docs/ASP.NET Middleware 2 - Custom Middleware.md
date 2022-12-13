# Schreiben einer eigenen Middleware

Im ersten Teil dieser Reihe wurde gezeigt, was eine Middleware eigentlich ist und welche zentrale Rolle diesen Bausteinen in der Abarbeitung von HTTP Requests zukommt. Mit ASP.NET Core gibt es drei grundsätzliche Arten, eine Middleware zu schreiben.

So können einfache Middlewarekomponenten mit Hilfe von Lambdaausdrücken inline formuliert werden oder zur besseren Wiederverwendbarkeit in eigene Klassen ausgelagert werden. Diese müssen dann entweder einer Konvention folgen oder ein Interface implementieren, um über Fabrikmuster während der Laufzeit erzeugt werden zu können. Jede dieser drei Möglichkeiten hat verschiedene Vor- und Nachteile, die wir nun im zweiten Teil dieser Reihe genauer betrachten werden.

## Inline-Middleware mit Hilfe von Lambdaausdrücken

In aller Regel wird man sich bei der Entwicklung einer ASP.NET-Core-Anwendung mit dem Schreiben von Razor-Seiten oder API-Controllern beschäftigen. Allerdings kann es Situationen geben, in denen die volle Funktionalität eines Controllers (und die damit verbundene Komplexität) nicht benötigt wird.

Das kann zum Beispiel der Fall sein, wenn man über einen einzelnen Endpunkt eine einfache Aktion ausführen möchte („a poor man’s IPC (inter-process communication)“), oder einen Endpunkt benötigt, der nichts Weiteres tut, als den aktuellen Wochentag zurückzuliefern. Auch das Hinzufügen eines Headers an zentraler Stelle kann als legitimer Anwendungsfall genannt werden.

Nun kann man für diesen Zweck einen eigenen API-Controller schreiben oder aber auf schlankere Methoden zurückgreifen und die Extension-Methoden *Run(), Map()* und *Use()* aus dem Namensraum *Microsoft.AspNetCore.Builder* verwenden*.* Diese Methoden werden nun anhand von verschiedenen Beispielen genauer betrachtet.

## Run-Methode

Mit der *Run*-Methode lässt sich eine einfache Middleware inline definieren und der HTTP Pipeline hinzufügen:

```c#
public static void Run(this IApplicationBuilder app, RequestDelegate handler);
```

Die Middleware hat dabei über den *RequestDelegate*-Parameter Zugriff auf den HTTP-Kontext, der alle Details zur Anfrage beinhaltet:

```c#
// A function that can process an HTTP request
public delegate Task RequestDelegate(HttpContext context);
```

In untenstehendem Beispiel wird der *Run*-Methode ein *RequestDelegate* in Form einer anonymen Methode übergeben, die für den Aufrufer Schere-Stein-Papier spielt und das Ergebnis einerseits als zusätzlichen Header (X-Rochambeau) und andererseits als HTTP Response Body zurückgibt (Listing 1).

Listing 1

```c#
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build(); 
 
// Define in-line middleware
app.Run(async (HttpContext context) => 
{
  var items = new string[] { "rock", "paper", "scissors" };
  var result = items[new Random().Next(items.Length)];
 
    // Add custom header 
    context.Response.Headers.Add("X-Rochambeau", result);
 
  // Write response body
  await context.Response.WriteAsync($"Rochambeau-Outcome: {result}");
});
 
// Poor delegate, you’ll never see an HTTP request :(
app.Run(async context => 
{
  Debug.WriteLine("You'll never see me!");
}); 
 
app.Run();
```

Eine auf diese Art definierte Middleware liefert immer eine Antwort zurück und stellt einen terminierenden Endpunkt dar, der die Pipeline kurzschließt. Da die Reihenfolge, in der Middlewarekomponenten registriert werden, von Bedeutung ist, wird jegliche nachfolgende Middleware nicht ausgeführt. Es ist also wichtig, den Aufruf von *Run* an das Ende der Pipeline-Konfiguration zu stellen. Der Aufruf der Middleware ist in Listing 2 zu sehen.

Listing 2

```cmd
> curl --include https://localhost:7226
HTTP/1.1 200 OK
Date: Mon, 03 Jan 2022 22:07:03 GMT
Server: Kestrel
Transfer-Encoding: chunked
X-Rochambeau: rock
 
Rochambeau-Outcome: rock
 
> curl --include https://localhost:7226/foobar
HTTP/1.1 200 OK
Date: Mon, 03 Jan 2022 22:07:04 GMT
Server: Kestrel
Transfer-Encoding: chunked
X-Rochambeau: scissors
 
Rochambeau-Outcome: scissors
```

Aufgrund der genannten Eigenschaften lässt sich die *Run*-Methode nur für wenige Anwendungsfälle sinnvoll einsetzen. So ergibt die Verwendung nur dort Sinn, wo ein einfacher Endpunkt verlangt wird, der immer, also unabhängig vom Anfragepfad, eine Antwort zurückliefern soll. Üblicher ist es hingegen, nur auf bestimmte Anfragepfade zu reagieren. Dafür eignen sich die *Map*- und *Use*-Methoden deutlich besser; sie werden im nächsten Abschnitt betrachtet.

## Map-/MapWhen-Methode

Im Gegensatz zur *Run*-Methode, erlauben *Map()* und *MapWhen()* die Aufsplittung der Pipeline in mehrere voneinander unabhängige Ausführungszweige. Die Entscheidung, welcher Pfad ausgeführt werden soll, wird anhand des Anfragepfades bzw. Prädikatwerts getroffen. Das ermöglicht unterschiedliches Verhalten für verschiedene Zweige der Pipeline:

```c#
// Branches the request pipeline based on matches of the given request path. 
// If the request path starts with the given path, the branch is executed. 
 
public static IApplicationBuilder Map(this IApplicationBuilder app, PathString pathMatch, Action<IApplicationBuilder> configuration)
```

Die *Map* Extension erwartet neben dem Pfad ein *Action* Delegate, das einen *IApplicationBuilder* als Parameter übernimmt und nichts zurückliefert.

Das nachfolgende Beispiel imitiert die ASP.NET-Core-Health-Check-Build-in-Middleware (Listing 3). Sie stellt einen konfigurierbaren Endpunkt bereit, mit dem der Gesundheitszustand einer Anwendung überwacht werden kann.

Listing 3

```c#
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build(); 
 
// Create branch
app.Map("/health", (IApplicationBuilder branchBuilder) =>
{
  // Terminal middleware
  branchBuilder.Run(async context =>
  {
    await context.Response.WriteAsync("Healthy");
  });
});
 
app.Map("/anotherbranch", (IApplicationBuilder branchBuilder) => 
{
  branchBuilder.UseStaticFiles();
  // Terminal middleware
  branchBuilder.Run(async context =>
  {
    await context.Response.WriteAsync("Terminated anotherbranch!");
  });
});
 
// Terminal middleware
app.Run(async context =>
{
  await context.Response.WriteAsync("Terminated main branch");
});
 
app.Run();
```

Da jeder Ausführungspfad für sich unabhängig ist, durchläuft ein Request entweder den einen oder den anderen Pfad, niemals aber beide. Das impliziert, dass Middleware, die einer pfadspezifischen Pipeline hinzugefügt wird, auch nur dort verfügbar ist. So kommt die *StaticFileMiddleware* im *Map*-Beispiel von oben auch nur im zweiten Branch (*/anotherbranch*) zur Anwendung. Listing 4 zeigt den Aufruf des *Map*-Beispiels.

Listing 4

```cmd
> curl https://localhost:7226/health
Healthy
> curl https://localhost:7226/health/foobar
Healthy
> curl https://localhost:7226/anotherbranch
Terminated anotherbranch
> curl https://localhost:7226/
Terminated main branch
> curl https://localhost:7226/foobar
Terminated main branch
```

Die *Map*-Methode erzeugt also mit Hilfe des *ApplicationBuilder* eine neue Pipeline. Das hat zur Folge, dass sich dadurch der Anfragepfad aus Sicht der Middleware verändert (Listing 5).

Listing 5

```c#
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build(); 
 
app.Map("/branch1", applicationBuilder =>
{
  applicationBuilder.Run(async context =>
  {
    var path = context.Request.Path; 
    var pathBase = context.Request.PathBase; 
    
    await context.Response.WriteAsync($"Path: {path} PathBase: {pathBase}");
  });
});
 
app.Run(async context =>
{
  var path = context.Request.Path; 
  var pathBase = context.Request.PathBase;
 
  await context.Response.WriteAsync($"Path: {path} PathBase: {pathBase}");
});
 
app.Run();
```

Der Aufruf macht deutlich, dass im Ausführungszweig der Pfad durch die *Map*-Methode verändert wurde. Die ursprüngliche Basis des Pfades wird dabei zwischengespeichert.

```cmd
> curl https://localhost:7014/branch1/segment1
Path: /segment1 PathBase: /branch1
 
> curl https://localhost:7014/anotherbranch/somesegment
Path: /anotherbranch/somesegment PathBase:
```

Die *Map*-Methode kann auch verschachtelt verwendet werden, um komplexere Strukturen zu schaffen (Listing 6). Obwohl das technisch möglich ist, ergibt es in der Praxis meist wenig Sinn. Stattdessen wird man auf einen vollwertigen API Controller zurückgreifen wollen. Listing 7 zeigt den entsprechenden Aufruf.

Listing 6

```c#
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build(); 
 
// Create branch
app.Map("/health", (IApplicationBuilder branchBuilder) =>
{
  // Create sub-branch
  branchBuilder.Map("/ping", (IApplicationBuilder anotherBranchBuilder) =>
  {
    // Terminal middleware
    anotherBranchBuilder.Run(async (HttpContext context) =>
    {
      await context.Response.WriteAsync("pong");
    });
  });
 
  // Terminal middleware
  branchBuilder.Run(async context =>
  {
    await context.Response.WriteAsync("Healthy");
  });
});
 
// Terminal middleware
app.Run(async context =>
{
  await context.Response.WriteAsync("Terminus");
});
 
app.Run();
```

Listing 7

```cmd
> curl https://localhost:7226/health
Healthy
> curl https://localhost:7226/health/foo
Healthy
> curl https://localhost:7226/health/ping
pong
> curl https://localhost:7226/health/ping/foo
pong
> curl https://localhost:7226/
Terminus
```

Listing 8

```c#
Func<HttpContext, bool> condition = (HttpContext context) =>
{
  return context.Request.Headers.ContainsKey("X-Custom-Header");
};
 
app.MapWhen(condition, (IApplicationBuilder branchBuilder) =>
{
  branchBuilder.Run(async (HttpContext context) =>
  {
    await context.Response.WriteAsync("Request contains X-Custom-Header");
  });
});
```

Neben der *Map*-Methode existiert noch die abgewandelte Form *MapWhen*. Sie erlaubt das bedingte Verzweigen basierend auf einem Prädikatwert (Listing 8) und dem Zustand des empfangenen *HttpContext*.

```c#
// Branches the request pipeline based on the result of the given predicate
 
public static IApplicationBuilder MapWhen(this IApplicationBuilder app, Func<HttpContext, bool> predicate, Action<IApplicationBuilder> configuration);
```

Folgende Verzweigung findet nur dann statt, wenn eine HTTP-Anfrage den Header *X-Custom-Header* beinhaltet.

Eine weniger ausführliche Variante könnte wie folgt aussehen:

```c#
app.MapWhen(context => context.Request.Headers.ContainsKey("X-Custom-Header"), branchBuilder =>
{
  branchBuilder.Run(async context => await context.Response.WriteAsync("Request contains X-Custom-Header"));
});
```

Eine äquivalente Variante zur pfadbasierten Verzweigung mittels *Map* könnte zum Beispiel wie folgt aussehen:

```c#
app.MapWhen(context => context.Request.Path.StartsWith("/today"), branchBuilder => 
{
  branchBuilder.Run(async (HttpContext context) =>
  {
    await context.Response.WriteAsync($"Today is {DateTime.UtcNow.DayOfWeek}");
  });
});
```

Selbstverständlich lassen sich auch Bedingungen verwenden, die nicht auf dem HTTP-Kontext beruhen:

```c#
app.MapWhen(_ => DateTime.UtcNow.DayOfWeek == DayOfWeek.Friday, (IApplicationBuilder branchBuilder) =>
{
  branchBuilder.Run(async (HttpContext context) =>
  {
    await context.Response.WriteAsync("Happy Weekend!");
  });
});
```

Die *MapWhen*-Variante ist also dann gegenüber der *Map*-Methode zu bevorzugen, wenn basierend auf dem Zustand des *HttpContext* eine Entscheidung über das Verzweigen der Pipeline getroffen werden soll, statt nur auf dem Pfad. Zusammenfassend lässt sich sagen, dass die Verwendung von *Map* und *MapWhen* dann Sinn ergibt, wenn man mehrere voneinander unabhängige Pipelines mit unterschiedlichem Verhalten benötigt. So lässt sich alles in einer Anwendung unterbringen. Der nächste Abschnitt betrachtet zwei weitere Erweiterungsmethoden, nämlich *Use* und *UseWhen.*

## Use/UseWhen Extension

Die *Use-* und *UseWhen*-Methoden können als die Schweizer Taschenmesser unter den bisher betrachteten Erweiterungsmethoden bezeichnet werden. Mit ihnen lassen sich eintreffende HTTP-Anfragen lesen, eine Antwort generieren oder diese an nachfolgende Middlewarekomponenten weiterreichen.

Schauen wir uns nun die recht kompliziert anmutende Signatur der *Use* Extension an, die wohl einer Erklärung bedarf:

```c#
// Adds a middleware delegate defined in-line to the application's 
// request pipeline
 
public static IApplicationBuilder Use(this IApplicationBuilder app, Func<HttpContext, RequestDelegate, Task> middleware)
```

Die *Use*-Extension erwartet ein Delegate vom Typ *Func<HttpContext, RequestDelegate, Task>.* Dieses Delegate kapselt eine Methode, die über zwei Parameter verfügt (*HttpContext* und *RequestDelegate*) und einen Task zurückgibt. Sie ist somit asynchron ausführbar. Das *RequestDelegate* repräsentiert dabei die nachfolgenden Middlewarekomponenten in der Pipeline. Das Beispiel in Listing 9 sollte die Verständlichkeit erleichtern.

Die erste Middleware fügt dem *HttpContext* den Text „Middleware1: Incoming“ hinzu. Anschließend übergibt sie die Ausführungskontrolle an die nachfolgende Middleware, indem sie das *RequestDelegate* asynchron aufruft und gleichzeitig den veränderten *HttpContext* übergibt (*await next.Invoke(context)*). Diese tut dasselbe und übergibt an die terminierende Middleware, die die Pipeline kurzschließt und die Ausführungs-Controller wieder zurück an die zweite Middleware übergibt usw.

Listing 9

```c#
var builder = WebApplication.CreateBuilder(args)
var app = builder.Build();
 
// First middleware
app.Use(async (context, next) =>
{
  await context.Response.WriteAsync("Middleware1: Incoming\n");
  await next.Invoke(context);
  await context.Response.WriteAsync("Middleware1: Outgoing\n");
});
 
// Second middleware
app.Use(async (context, next) =>
{
  await context.Response.WriteAsync("Middleware2: Incoming\n");
  await next.Invoke(context);
  await context.Response.WriteAsync("Middleware2: Outgoing\n");
});
 
// Terminal middleware
app.Run(async context =>
{
  await context.Response.WriteAsync("Terminal middleware\n");
});
 
app.Run();
```

## Achtung!

Die statische Klasse *Microsoft.AspNetCore.Builder.UseExtensions* beinhaltet zusätzlich eine überladene Variante mit der Signatur *public static IApplicationBuilder Use (this IApplicationBuilder app, Func<HttpContext, Func<Task>, Task> middleware)*. Diese erwartet statt einem *RequestDelegate* ein *Func<Task>* Delegate. Microsoft empfiehlt für bessere Performance die Verwendung der *RequestDelegate*-Variante. Also *await next.Invoke(context)* statt *await next.Invoke()!*

Der entsprechende Aufruf sieht folgendermaßen aus:

```cmd
> curl https://localhost:7014
Middleware1: Incoming
Middleware2: Incoming
Terminal middleware
Middleware2: Outgoing
Middleware1: Outgoing
```

Analog zu *MapWhen* erlaubt *UseWhen* die bedingte Verwendung von Middleware basierend auf einem Prädikatwert:

```c#
// Conditionally creates a branch in the request pipeline that is 
// rejoined to the main pipeline
 
public static IApplicationBuilder UseWhen(this IApplicationBuilder app, Func<HttpContext, bool> predicate, Action<IApplicationBuilder> configuration)
```

Nachfolgendes Beispiel verwendet nur für Anfragen an */images* die HTTP-Logging-Middleware. Unabhängig vom Anfragepfad wird zusätzlich der Header *X-Today-Is* mit dem aktuellen Wochentag hinzufügt. Dieses Beispiel soll verdeutlichen, dass bei der Verwendung von *UseWhen* (Listing 10), die nachfolgende Middleware immer aufgerufen wird. Also auch dann, wenn der Prädikatwert *false* zurückliefert. Das steht im Gegensatz zu *MapWhen*, bei dem durch den *ApplicationBuilder* ein eigener Ausführungszweig bzw. eine eigene Pipeline erzeugt wird.

Listing 10

```c#
app.UseWhen(context => context.Request.Path.StartsWithSegments("/images"), applicationBuilder =>
{
  applicationBuilder.UseHttpLogging();
});
 
// Gets called regardless of predicate of UseWhen()
app.Use(async (context, next) =>
{
  context.Response.Headers.Add("X-Today-Is", DateTime.UtcNow.DayOfWeek.ToString());
 
  await next.Invoke(context);
});
```

## Konventionsfolgende Middleware

Wie eingangs erwähnt, kann eine ASP.NET-Core-Middleware auf drei Arten geschrieben werden. Das Schreiben mittels Inline-Lambdaausdrücken unter Verwendung der *Run-*, *Map-* und *Use*-Methoden haben wir bereits kennengelernt. Diese Herangehensweise hat uns schnell einfache Middleware schreiben lassen. Allerdings kann es bei der Verwendung der Extension-Methoden schnell unübersichtlich werden. Stattdessen wird man bei umfangreichen Middlewarekomponenten die Funktionalität in eigene Klassen auslagern wollen. Schauen wir uns nun ein solches Beispiel an. Die untenstehende Middleware fügt jeder Response einen Header mit zufälligem Wert hinzu. In Listing 11 ist die konventionsfolgende Middleware zu sehen.

Listing 11

```c#
public class RochambeauMiddleware
{
  private readonly RequestDelegate _next; 
 
  public RochambeauMiddleware(RequestDelegate next) => _next = next;
 
  public async Task InvokeAsync(HttpContext context)
  {
    var items = new string[] { "rock", "paper", "scissors" };
    var result = items[new Random().Next(items.Length)];
 
    context.Response.Headers.Add("X-Rochambeau-Outcome", result);
 
    await _next.Invoke(context);
  }
}
```

## Die Konvention

Die Middlewareklasse muss dabei nicht zwingend von einer Basisklasse ableiten oder ein Interface implementieren. Stattdessen muss diese nur einer bestimmten Konvention folgen. Diese Konvention schreibt einen öffentlichen Konstruktor vor, der ein *RequestDelegate* erwartet. Ebenso muss es eine öffentliche Methode geben, die einen *HttpContext* als Parameter entgegennimmt. Untenstehende Konstruktorsignaturen sind gültige Varianten, die genaue Position des *RequestDelegate*-Parameters ist dabei nicht relevant:

```c#
public MyMiddleware(RequestDelegate next)
public MyMiddleware(RequestDelegate next, IService service)
public MyMiddleware(ILogger<MyMiddleware> logger, RequestDelegate next, IService service)
```

Was die Signatur der öffentlichen Methode betrifft, so sind untenstehende Varianten gültig. Im Gegensatz zum Konstruktor kann die Reihenfolge der Parameter nicht frei gewählt werden und der *HttpContext* muss zwingend an erster Stelle stehen.

```c#
public async Task InvokeAsync(HttpContext context);
public async Task InvokeAsync(HttpContext context, IServiceA serviceA);
public async Task Invoke(HttpContext context);
public async Task Invoke(HttpContext context, IServiceA serviceA);
```

Das genaue Befolgen dieser Konvention ist zwingend notwendig, da ASP.NET Core die Middlewareklasse mittels Introspektion (Reflection) instanziiert. Dadurch wird diese flexibler in ihrer Anwendbarkeit und ermöglicht so einfaches Einbringen von Abhängigkeiten über die Methode (Dependency Injection). Das wäre mit einem implementierten Interface nicht möglich. Denn Schnittstellen oder überschriebene Methoden aus Basisklassen würden die Methodensignatur strikt vorgeben und das Einbringen von Abhängigkeiten über die Methode *Invoke* bzw. *InvokeAsync* nicht erlauben.

#### 10 Tipps zur Implementierung von DevSecOps in Unternehmen

Mit immer kürzeren Releasezyklen Schritt zu halten und gleichzeitig Sicherheitsrisiken effektiv zu begegnen, müssen Application-Security und Datenschutz von Beginn an integraler Bestandteil des Softwareentwicklungsprozesses sein.

## Vererbung und konventionsfolgende Middleware

Obwohl eine konventionsfolgende Middleware nicht von einer Basisklasse ableiten muss, kann sie das natürlich trotzdem tun! Solange die Klasse alle Konventionen erfüllt, wird sie von ASP.NET Core als Middleware erkannt und als solche entsprechend instanziiert.

## Lebensdauer einer konventionsfolgenden Middleware

Weiter ist hervorzuheben, dass ASP.NET Core konventionsfolgende Middlewaretypen mit einer Singleton Lifetime im DI-Container registriert.

## Lebensdauer eines Objekts

Wird der DI-Container nach einer Instanz gefragt, definiert die konfigurierte Lebensdauer, ob der Container eine neue oder eine bestehende Instanz zurückgibt. ASP.NET Core kennt drei verschiedene Lebensdauern, die während der Registration am DI-Container bestimmt werden.

- *Singleton:* Der Container liefert für jede Anfrage die gleiche Instanz zurück.
- *Scoped:* Innerhalb eines Abschnitts gibt der Container immer die gleiche Instanz zurück.
- *Transient:* Für jede Anfrage gibt der Container eine neue Instanz zurück.

Daraus folgt, dass keine zustandsorientierten Abhängigkeiten (Scoped Dependencies) über den Konstruktor eingebracht werden sollten. Das stellt im Kontext der DI-Prinzipien ein Antipattern dar, das von Mark Seeman [1] als Captive Depedencies, also als gefangene Abhängigkeiten beschrieben wird. Dabei halten Services mit einer längeren Lebenszeit Abhängigkeiten mit einer kürzeren gefangen und dehnen deren Lebenszeit damit künstlich aus. Listing 12 zeigt Captive Depedencies.

Listing 12

```c#
public class Service : IService
{
  private readonly IDependency _dependency; 
 
  public Service(IDependency dependency) => _dependency = dependency;
}
 
// Service registration causing captive dependencies
services.AddSingleton<IService, Service>();
services.AddScoped<IDependecy, Dependency>();
```

Mit dem DI-Container von Microsoft (*Microsoft.Extensions.DependencyInjection*) ist man hier allerdings auf der sicheren Seite, da dieser mit einer sogenannten Scope Validation ausgestattet ist. Diese entdeckt Registrationen dieser Art und löst eine entsprechende Exception aus, vorausgesetzt die Anwendung wird in der Development-Umgebung ausgeführt:

```
Some services are not able to be constructed (Error while validating the service descriptor 'ServiceType: IService Lifetime: Singleton ImplementationType: Service': Cannot consume scoped service 'IDependency' from singleton 'IService'.)
```

Kurzlebige Abhängigkeiten (transient), die über den Konstruktor eingebracht werden, sind dagegen weniger problematisch. Zumindest dann nicht, wenn sie Microsofts Definition einer solchen folgen und zustandslos sind. Trotzdem ist es konsequenter, sie über die Methode einzubringen. Zustandsorientierte (scoped) und kurzlebige (transient) Abhängigkeiten sollten also nur über die *Invoke-* bzw. *InvokeAsync*-Methode eingebracht werden. In Listing 13 wird das Einbringen von Scoped Services über die Methode gezeigt.

Listing 13

```c#
// Program.cs
services.AddSingleton<IAmSingleton, SingletonService>();
services.AddTransient<IAmTransient, TransientService>();
services.AddScoped<IAmScoped, ScopedService>();
 
// MyMiddleware.cs
public class MyMiddleware
{
  public readonly RequestDelegate _next;
  public readonly IAmSingleton _singletonService;
  public readonly IAmTransient _transientService; 
 
  public MyMiddleware(RequestDelegate next, IAmSingleton service1, IAmTransient service2) 
  {
    _next = next; 
    _singletonService = service1;
    _transientService = service2;
  } 
 
  public async Task InvokeAsync(HttpContext context, IAmScoped service)
  {
    // ...
    await _next(context); 
  }
}
```

Weiter bleibt anzumerken, dass konventionsfolgende Middlewaretypen Thread-safe sein müssen, da durch gleichzeitige Netzwerkanfragen auch mehrere Threads auf eine Middlewareinstanz zugreifen können.

## Registrieren und Konfigurieren der Middleware

Nun muss die Middleware noch an geeigneter Stelle mit der Pipeline registriert werden. Dazu wird man sich einer der beiden *UseMiddleware*-Methoden bedienen.

```c#
public static IApplicationBuilder UseMiddleware (IApplicationBuilder app, Type middleware, params object?[] args);
```

oder

```c#
public static IApplicationBuilder UseMiddleware<TMiddleware> (this IApplicationBuilder app, params object?[] args);
```

Nachfolgendes Beispiel verdeutlicht die Benutzung (Listing 14).

Listing 14

```c#
// Middleware1.cs
public class Middleware1
{
  private readonly RequestDelegate _next;
 
  public Middleware1(RequestDelegate next) => _next = next;
 
  public async Task InvokeAsync(HttpContext context)
  {
    await context.Response.WriteAsync("Middleware1: Incoming\n");
    await _next.Invoke(context);
    await context.Response.WriteAsync("Middleware1: Outgoing\n");
  }
}
 
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
 
// Register convention-based middleware
app.UseMiddleware<Middleware1>();
 
// Register in-line middleware
app.Use(async (context, next) =>
{
  await context.Response.WriteAsync("Middleware2: Incoming\n");
  await next.Invoke(context);
  await context.Response.WriteAsync("Middleware2: Outgoing\n");
});
 
// Terminal middleware
app.Run(async context =>
{
  await context.Response.WriteAsync("Terminal middleware\n");
});
 
app.Run();
```

Bei genauer Betrachtung der *UseMiddleware*-Signatur fällt auf, dass diese einen optionalen Parameter *args* aufweist. Darüber lassen sich Konfigurationsparameter an den Middleware Konstruktor übergeben (Listing 15). Die Parameter können dabei primitive Datentypen als auch komplexere Konfigurationsobjekte sein.

Listing 15

```c#
// Middleware1.cs
public class Middleware1
{
  private readonly RequestDelegate _next;
  private readonly int _count;
  
  public Middleware1(RequestDelegate next, int count)
  {
    _next = next;
    _count = count;
  } 
 
  public async Task InvokeAsync(HttpContext context)
  {
    for (var i = 0; i < _count; i++)
      await context.Response.WriteAsync("ASP.NET Core rocks!!\n");
    
      await _next.Invoke(context);
  }
}
 
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
 
// Register & configure convention-based middleware
app.UseMiddleware<Middleware1>(3);
 
// Terminal middleware
app.Run(async context =>
{
  await context.Response.WriteAsync("Terminal middleware\n");
});
 
app.Run();
```

Möchte man seine Middleware anderen Entwicklern als Bibliothek zur Verfügung stellen, wird man i. d. R eine Extension-Methode mitliefern wollen, die sich in die Konvention der ASP.NET-Core-Built-in-Middlewarekomponenten einreiht und als Fassade dient (Listing 16).

Listing 16

```c#
// ApplicationBuilderExtensions.cs
public static class ApplicationBuilderExtensions
{
  public static IApplicationBuilder UseMyCoolMiddleware(this IApplicationBuilder app, int count)
  {
    return app.UseMiddleware<Middleware1>(count);
  }
}
 
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
 
// Register and configure convention-based middleware
app.UseMyCoolMiddleware(count: 3);
 
// Terminal middleware
app.Run(async context =>
{
  await context.Response.WriteAsync("Terminal middleware\n");
});
 
app.Run();
```

Wie wir gesehen haben, wird man komplexere Middlewareaufgaben in eine dedizierte Klasse auslagern wollen. So bleibt der Quelltext strukturiert und übersichtlich. Die konventionsfolgende Middleware wird dabei von ASP.NET Core mittels Introspektion als Singleton instanziiert und bedarf deshalb keiner Registration mit dem DI-Container.

## Fabrikmustererzeugte Middleware

Seit der Einführung von ASP.NET Core 2.0 gibt es eine neue Art, Middleware zu schreiben. Nämlich Middlewareklassen, die von ASP.NET Core per Fabrikmethode erzeugt werden (Factory Method Pattern). Die Schnittstellen *IMiddleware* und *IMiddlewareFactory* stellen dabei den Kern dar. Letztere definiert die Fabrikmethode *Create*, mit deren Hilfe ASP.NET Core neue Middlewareinstanzen vom Typen *IMiddleware* während der Laufzeit für jede Anfrage neu erzeugt.

```c#
// Request handling method
Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next);
 
// Creates a middleware instance for each request
public IMiddleware? IMiddlewareFactory.Create(Type middlewareType);
```

Schauen wir uns nun ein Beispiel an. Die nachfolgende Middleware implementiert das *IMiddleware*-Interface und erzeugt für jede generierte HTTP Response einen Logeintrag (z. B. *GET /foobar => 200*). Listing 17 zeigt als Beispiel eine fabrikmustererzeugte Middleware.

Listing 17

```c#
// LoggingMiddleware.cs
public class LoggingMiddleware : IMiddleware
{
  private readonly ILogger _logger;
  
  public LoggingMiddleware(ILoggerFactory loggerFactory)
  {
    _logger = loggerFactory.CreateLogger<LoggingMiddleware>();
  }
  
  // Fulfill interface contract
  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    // Pass control to next middleware
    await next.Invoke(context);
    
    // Do some logging on returning call
    _logger.LogInformation($"{context.Request?.Method} {context.Request?.Path.Value} => {context.Response?.StatusCode}");
  }
}
 
// Program.cs
var builder = WebApplication.CreateBuilder(args);
 
ConfigureConfiguration(builder.Configuration);
ConfigureServices(builder.Services);
 
var app = builder.Build();
 
ConfigureMiddleware(app, app.Services);
ConfigureEndpoints(app, app.Services);
 
app.Run();
 
void ConfigureConfiguration(ConfigurationManager configuration) {} 
void ConfigureServices(IServiceCollection services)
{
  services.AddScoped<LoggingMiddleware>();
}
void ConfigureMiddleware(IApplicationBuilder app, IServiceProvider services)
{
  app.UseMiddleware<LoggingMiddleware>();
  
  app.Run(async context =>
  {
    await context.Response.WriteAsync("Terminal middleware\n");
  });
}
void ConfigureEndpoints(IEndpointRouteBuilder app, IServiceProvider services){}
```

Über die *InvokeAsync*-Methode wird der HTTP-Kontext und die nachfolgende Middleware anhand des *RequestDelegate* überreicht. Anschließend übergibt die *LoggingMiddleware* die Ausführungskontrolle sowie den HTTP-Kontext direkt an die nachfolgende Middleware, ohne dabei selbst eine Aktion auszuführen. Erst nachdem vom Endpunkt eine Antwort generiert wurde, tritt die *LoggingMiddleware* in Aktion und erzeugt basierend auf der HTTP Response einen Logeintrag.

## Lebensdauer und Registration

Das Besondere an dieser Art von Middleware besteht in ihrer Lebensdauer. So wird anhand der internen *IMiddlewareFactory*-Implementierung für jede HTTP-Anfrage eine neue Instanz der *LoggingMiddleware* erzeugt. Dieses Verhalten steht im Kontrast zu einer konventionsfolgenden Middleware, die beim Starten der ASP.NET-Core-Anwendung mit einer Singleton-Lebensdauer erzeugt werden. Sie werden also für jede Clientanfrage wiederverwendet.

Weiter müssen fabrikmustererzeugte Middlewareklassen explizit am DI-Container registriert werden:

```c#
services.AddScoped<LoggingMiddleware>();
```

Anschließend können sie wie gewohnt mit Hilfe von *UseMiddleware* der Pipeline hinzugefügt werden:

```c#
app.UseMiddleware<LoggingMiddleware>();
```

## Einbringen von Abhängigkeiten

Durch das Implementieren der *IMiddleware*-Schnittstelle können keine zusätzlichen Abhängigkeiten über die *InvokeAsync*-Methode eingebracht werden, da ansonsten der vorgegebene Kontrakt nicht erfüllt werden würde.

Allerdings lassen sich über den Konstruktor, vor allem auch bereichsbezogene Abhängigkeiten (Scoped Services), einbringen. Dies ist bei konventionsfolgenden Middlewareklassen aufgrund des so entstehenden Antipatterns (Captive Dependencies) nicht zu empfehlen. Es folgt ein Beispiel zur Verdeutlichung, bei dem ein Singleton Service und Scoped Service über den Konstruktor eingebracht werden (Listing 18). Die Abhängigkeiten müssen entsprechend mit dem DI-Container registriert werden.

Listing 18

```c#
// LoggingMiddleware.cs
public class LoggingMiddleware : IMiddleware
{
  private readonly ILogger _logger;
  private readonly IAmScoped _service1; 
  private readonly IAmSingleton _service2;
  
  public LoggingMiddleware(ILoggerFactory loggerFactory, IAmScoped service1, IAmSingleton service2)
  {
    _logger = loggerFactory.CreateLogger<LoggingMiddleware>();
    _service1 = service1; 
    _service2 = service2; 
  }
  
  // Fulfill interface contract
  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    // make use of service1/service2
 
    // Pass control to next middleware
    await next.Invoke(context);
    
    // Do some logging on returning call
    _logger.LogInformation($"{context.Request?.Method} {context.Request?.Path.Value} => {context.Response?.StatusCode}");
    }
}
 
// Program.cs
var builder = WebApplication.CreateBuilder(args);
 
ConfigureConfiguration(builder.Configuration);
ConfigureServices(builder.Services);
 
var app = builder.Build();
 
ConfigureMiddleware(app, app.Services);
ConfigureEndpoints(app, app.Services);
 
app.Run();
 
void ConfigureConfiguration(ConfigurationManager configuration) {} 
void ConfigureServices(IServiceCollection services)
{
  services.AddScoped<IAmScoped, Service1>(); 
  services.AddSingleton<IAmSingleton, Service2>();
  services.AddScoped<LoggingMiddleware>();
}
void ConfigureMiddleware(IApplicationBuilder app, IServiceProvider services)
{
  app.UseMiddleware<LoggingMiddleware>();
  
  app.Run(async context =>
  {
    await context.Response.WriteAsync("Terminal middleware\n");
  });
}
void ConfigureEndpoints(IEndpointRouteBuilder app, IServiceProvider services){}
```

## Übergabe von Parametern

Wir haben bereits erfahren, wie man mit *UseMiddleware* eine Komponente an eine geeignete Position der Pipeline hinzufügen kann. Auch haben wir gesehen, wie sich primitive Datentypen an eine konventionsfolgende Middleware für Konfigurationszwecke übergeben lassen.

Für die fabrikmustererzeugte Middleware existiert hier eine kleine Einschränkung. Es lassen sich nicht ohne weiteres primitive Datentypen wie Strings oder Integer über die *UseMiddleware*-Methode einbringen. Allerdings hat man mehrere Alternativen zur Hand.

Zum einen kann die Registrierung der Middleware am DI-Container geändert werden:

```c#
services.AddScoped(x => ActivatorUtilities.CreateInstance<LoggingMiddleware>(x, 2, ... ));
```

Zum anderen können die Konfigurationsparameter auch in einer eigenen Klasse gekapselt und dann regulär mit dem DI-Container registriert werden. Das gilt natürlich auch für konventionsfolgende Middleware. Die Verwendung einer Konfigurationsklasse zeigt Listing 19.

Listing 19

```c#
// MyConfig.cs
public class MyConfig { public int Count { get; set; }}
 
// MyMiddleware.cs
public class MyMiddleware : IMiddleware
{
  private readonly MyConfig _config
  public class MyMiddleware(MyConfig config) => _config = config;
  // ... 
}
 
// Program.cs
builder.Services.AddSingleton<MyConfig>();
builder.Services.AddScoped<MyMiddleware>();
// ... 
```

## Fazit

Im ersten Teil dieser Serie haben wir gelernt, was Middleware eigentlich ist und was sie für eine ASP. NET-Core-Anwendung leisten kann. In diesem zweiten Teil haben wir uns mit dem Schreiben von Middleware beschäftigt. Im dritten und letzten Teil werden wir uns dann dem Testing der Pipeline widmen.

Zu guter Letzt soll die nachstehende Auflistung noch einmal die wichtigsten Aspekte zusammenfassen und die Vor- und Nachteile verdeutlichen.

### Run, Map und Use

sind unter Umständen schwierig zu lesen und zu debuggen, schwierig zu testen

- nützlich für einfache Szenarien
- sollte dann verwendet werden, wenn die geforderte Funktionalität sehr einfach ist und keine Abhängigkeiten benötigt

### Konventionsfolgende Middleware

ermöglicht einfache Unit- und Integrationstests

- Middlewareklasse muss nicht explizit am DI-Container registriert werden, wohl aber ihre Abhängigkeiten
- für jede HTTP-Anfrage wird dieselbe Middlewareinstanz verwendet (Singleton)
- Scoped Dependencies können nur via Methode eingebracht werden (Captive Dependencies)
- Singleton Dependencies können via Konstruktor oder Methode eingebracht werden
- primitive Datentypen können als Konfigurationsparameter an *UseMiddleware* übergeben werden
- Sollte dann verwendet werden, wenn die Middleware keine Abhängigkeiten hat oder sichergestellt ist, dass diese Singletons sind

### Fabrikmustererzeugte Middleware

ermöglicht einfache Unit- und Integrationstests

- Middlewareklasse sowie ihre Abhängigkeiten müssen explizit am DI-Container registriert werden
- für jede HTTP-Anfrage wird eine neue Middlewareinstanz verwendet (Scoped)
- Abhängigkeiten können (nur) via Konstruktor eingebracht werden (Singleton und Scoped)
- primitive Datentypen können nicht direkt via *UseMiddleware* übergeben werden
- sollte dann verwendet werden, wenn die Middleware Abhängigkeiten zu Scoped Services hat, da so das Problem der Captive Dependencies vermieden wird