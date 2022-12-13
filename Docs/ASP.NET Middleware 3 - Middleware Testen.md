# Testen von Middlewarekomponenten

In den vorangegangenen Teilen dieser Serie haben wir uns mit den Grundlagen und Voraussetzungen sowie dem eigenen Schreiben von Middlewarekomponenten beschäftigt. Ein Aspekt wurde dabei allerdings bewusst ausgelassen, nämlich die Testbarkeit. Darum soll es nun in diesem letzten Teil gehen.

Das Schreiben von Unit- und Integrationstests ist eine Kunstform für sich. Nicht selten nehmen Diskussionen darüber religiöse Formen an. Welche Testabdeckung sollte die Anwendung aufweisen? Sollte man rigoros dem vom Test-driven Development stammenden Red-Green-Refactor-Ansatz folgen und zuerst die Tests und erst dann die Implementierung schreiben?

Diese Fragen muss jeder Entwickler beziehungsweise jedes Team für sich selbst beantworten. Aber es herrscht wohl allgemein Einigkeit darüber, dass Tests die Codequalität deutlich und auch messbar erhöhen. So hat eine Studie von Microsoft [1] ergeben, dass sich die Fehlerdichte (engl. Defect Density), also das Verhältnis zwischen Anzahl an Bugs zu Codezeilen, durch den Einsatz von TDD zwischen 40 und 90 Prozent verringern lässt.

Die zeitliche Investition in Tests lohnt sich also, und so sollten auch die eigenen Middlewareklassen ausgiebigen Tests unterzogen werden. Allein schon deswegen, weil eine fehlerhafte Middleware die gesamte ASP.NET-Core-Anwendung in einen unbenutzbaren Zustand versetzen kann. Um die korrekte Funktionalität zu gewährleisten, sollte diese einerseits in Isolation (in Form eines Unit-Tests), aber auch mit der gesamten Middleware-Pipeline (in Form von Integrationstests) überprüft werden.

## Anschauungsbeispiel

Als Anschauungsbeispiel soll eine einfache Health-Check-Middleware dienen, die verschiedenen Tests unterzogen wird. Sie wurde der gleichnamigen und integrierten ASP.NET-Core-Middleware nachempfunden.

Im Beispiel wird die Abhängigkeit *IHealthCheckService* eingebracht, die einerseits Auskunft über den aktuellen Zustand der Anwendung gibt und andererseits rudimentäre Kennzahlen zur CPU und Speichernutzung zurückliefert. Die Middleware antwortet auf Anfragen an den Pfad */health* und fügt das Ergebnis des Health-Checks in Form eines Headers der HTTP-Antwort hinzu (*X-Health-Check*, Listing 1). Das direkte Definieren des Pfads in der Middleware wird in der Praxis nicht empfohlen und dient hier rein zur Demonstration. Stattdessen sollte man auf Endpoint-Routing zurückgreifen, was deutlich mehr Flexibilität bietet.

Listing 1

```c#
// IHealthCheckService.cs
public interface IHealthCheckService
{
  public bool IsHealthy();
 
  public string GetMetrics();
}
 
// HealthCheckService.cs
public class HealthCheckService : IHealthCheckService
{
  public virtual bool IsHealthy() => true;
 
  public virtual string GetMetrics()
  {
    var process = Process.GetCurrentProcess();
     
    return $"CPU[ms]: {process.TotalProcessorTime.Milliseconds}, MEM[b]: {process.WorkingSet64}";
  }
}
 
// HealthCheckMiddleware.cs
public class HealthCheckMiddleware
{
  private readonly RequestDelegate _next;
 
  public HealthCheckMiddleware(RequestDelegate next) => _next = next;
 
  public async Task InvokeAsync(HttpContext context, IHealthCheckService service)
  {
    if (context.Request.Path.StartsWithSegments("/health"))
    {
      context.Response.Headers.Add("X-Health-Check", service.IsHealthy() ? "Healthy" : "Degraded");
 
      await context.Response.WriteAsync(service.GetMetrics());
    }
    else
    {
      await _next.Invoke(context);
    }
  }
}
```

## Unit Testing

Das Ziel eines Unit-Tests besteht darin, eine Komponente isoliert und ohne Interaktion mit anderen Modulen auf ihre korrekte Funktionalität zu untersuchen. In der Regel hat eine Middleware sowohl domänenspezifische Abhängigkeiten (wie *IHealthCheckService* im aufgeführten Beispiel) als auch Abhängigkeiten zum Framework (*HTTPContext*, Request Delegate). Diese müssen für eine vollständige Isolation mit Hilfsobjekten simuliert werden.

## Erzeugen eines künstlichen HTTPContext

Im ersten Teil der Serie wurde aufgezeigt, wie Kestrel eine Clientanfrage in ein *HTTPContext*-Objekt verpackt. Dieses Objekt wird in Form der *HttpContext*-Klasse an die Pipeline übergeben und von Middleware zu Middleware durchgereicht. Betrachtet man die Signatur dieser Klasse, stellt man fest, dass sie als *abstract* markiert ist und somit für einen Test nicht direkt instanziiert werden kann. Zwar könnte man für Testzwecke einen eigenen Typ davon ableiten, allerdings ist aufgrund der Komplexität und des Initialisierungsaufwands davon abzuraten.

Eine deutlich komfortablere Variante stellt die Verwendung der Standardimplementierung *DefaultHttpContext* dar, die bereits von *HttpContext* ableitet:

```c#
var context = new DefaultHttpContext();
context.Request.Path = "/health";
// ... 
```

Alternativ lässt sich der *HTTPContext* auch mit Mocking-Bibliotheken wie Moq simulieren (*var ctxMock = Mock<HttpContext>()*).

## Ein einfaches Beispiel

Der Unit-Test in Listing 2 überprüft, ob die Pipeline auch wirklich kurzgeschlossen wird, falls sich eine HTTP-Anfrage an den Pfad */health* richtet. Dazu wird der Anfragepfad manuell im *HTTPContext* gesetzt, um die Clientanfrage zu simulieren. Die nachfolgende Middleware (*RequestDelegate*) und der *HealthCheckService* werden durch einen Mock ersetzt. Da die Pipeline beim passenden Anfragepfad kurzgeschlossen werden muss, sollte eine nachfolgende Middleware nicht mehr aufgerufen werden. Dieser Fakt wird mit dem Assert überprüft.

Listing 2

```c#
[Fact]
public async Task HealthCheckMiddleware_should_terminate()
{
  // arrange
  var context = new DefaultHttpContext();
  context.Request.Path = "/health";
  
  var serviceMock = new Mock<HealthCheckService>();
  serviceMock.Setup(s => s.GetMetrics()).Returns("Fake Metric");
  
  var nextMock = new Mock<RequestDelegate>();
  
  var middleware = new HealthCheckMiddleware(nextMock.Object);
 
  // act
  await middleware.InvokeAsync(context, serviceMock.Object);
 
  // assert
  nextMock.Verify(n => n.Invoke(context), Times.Never);
}
```

Basierend auf der vorgestellten Beispielmiddleware, könnten weitere Unit-Tests folgende Aspekte untersuchen. So zum Beispiel, dass ...

- ... der Header korrekt gesetzt wird.
- ... der Header nicht gesetzt wird, falls der Pfad von */ health* abweicht.
- ... der eingebrachte Service bei einer Anfrage auf */ health* aufgerufen wird.
- ... der Response Body bei einer Anfrage auf */health* Kennzahlen zurückliefert.
- ... der eingebrachte Service für andere Pfade nicht aufgerufen wird.
- ... eine mögliche nachfolgende Middleware fehlerfrei aufgerufen wird.
- ... eine Exception geworfen wird, falls die Middleware einen nicht instanziierten Service aufruft.

Der vorgestellte Unit-Test hat die Middleware auf ihr Verhalten hin untersucht. Möchte man überprüfen, ob der Service auch tatsächlich Kennzahlen in den Response Body schreibt, wird es etwas aufwendiger.

## Lesen der HTTP-Payload

Auf die Payload oder den Body einer HTTP-Nachricht kann mit Hilfe von Streams (*System.IO*) lesend und schreibend zugegriffen werden. Diese Streams werden über die Properties *HttpContext.Request.Body* beziehungsweise *HttpContext.Response.Body* bereitgestellt. Zur Laufzeit sind diese Streams Typen der Klassen *HttpRequestStream* beziehungsweise *HttpResponseStream* und können über die in der Dokumentation beschriebenen Methoden benutzt werden. Erzeugt man für Testzwecke nun einen Kontext mit *DefaultHttpContext*, so werden diese Properties mit dem internen Typ *NullStream* instanziiert:

```c#
var context = new DefaultHttpContext(); 
 
// Both are of type System.IO.Stream+NullStream
var requestBodyType = context.Request.Body.GetType().FullName;
var responseBodyType = context.Response.Body.GetType().FullName;
```

Da ein *NullStream* alle geschriebenen Daten verwirft, muss er für Testzwecke explizit durch einen *MemoryStream* ersetzt werden. Nur so kann auf die Payload zugegriffen werden. Der Unit-Test in Listing 3 veranschaulicht die Benutzung. Mit ein paar wenigen Unit-Tests können wir so grundlegende Aspekte einer Middlewareklasse bereits gut untersuchen.

Listing 3

```c#
[Fact]
public async Task HealthCheckMiddleware_should_return_metrics()
{
  // arrange
  var bodyStream = new MemoryStream();
  
  var context = new DefaultHttpContext();
  context.Request.Path = "/health";
  context.Response.Body = bodyStream;
 
  var nextMock = new Mock<RequestDelegate>();
  var serviceMock = new Mock<HealthCheckService>();
  serviceMock.Setup(s => s.GetMetrics()).Returns("Fake Metric");
  
  var middleware = new HealthCheckMiddleware(nextMock.Object);
 
  // act
  await middleware.InvokeAsync(context, serviceMock.Object);
 
  // assert
  bodyStream.Seek(0, SeekOrigin.Begin);
  using var stringReader = new StreamReader(bodyStream);
  var body = await stringReader.ReadToEndAsync();
  
  Assert.Equal("Fake Metric", body);
}
```

## Integrations-Testing

Mit verschiedenen Unit-Tests haben wir nun sichergestellt, dass die Middleware in Isolation funktioniert. Allerdings wissen wir noch nicht, ob diese auch im Zusammenspiel mit anderen Middlewarekomponenten so funktioniert, wie wir es erwarten. Dazu werden wir mindestens einen Integrationstest benötigen. Aber wie testet man nun das Zusammenspiel der eigenen Middlewareklasse mit dem ASP.NET Core Framework und der Pipeline?

Dazu stellt Microsoft mit den Bibliotheken Microsoft.AspNetCore.TestHost und Microsoft.AspNetCore.Mvc.Testing zwei praktische Werkzeuge bereit, mit denen sich das Schreiben von Integrationstests für ASP. NET-Core-Anwendungen und Middlewarekomponenten stark vereinfachen lässt.

## In-Memory-ASP.NET-Core-Server

Die Bibliothek TestHost beinhaltet einen In-Memory-Host (*TestServer*), mit dem sich Middlewareklassen in Isolation testen lassen. Dieser ermöglicht die Erstellung einer HTTP Request Pipeline, die nur die für den Test notwendigen Komponenten beinhaltet. So können spezifische Anfragen an die Middleware gesendet und es kann deren Verhalten untersucht werden.

Die Kommunikation zwischen Testclient und Testserver findet dabei ausschließlich im RAM statt. Das bietet den Vorteil, dass man sich als Entwickler nicht mit Themen wie TCP-Port-Management oder TLS-Zertifikaten herumschlagen muss. Zusätzlich wird die Laufzeit der Tests reduziert. Auch fließen mögliche Exceptions, die von der Middleware geworfen werden, direkt an den aufrufenden Test zurück. Zudem lässt sich der *HttpContext* direkt im Test manipulieren.

Der Test in Listing 4 zeigt, wie sich mit der Extension-Methode *UseTestServer()* die Testumgebung erzeugen und die zu testende Middleware einbinden lässt. Die Bibliothek TestHost stellt neben dem *TestServer* auch einen *TestClient* bereit, der mit *GetTestClient()* instanziiert werden kann. Mit Hilfe von *GetAsync("/health")* wird ein HTTP-*GET-*Request an die Pipeline gesendet und die Response für den Assert verwendet.

Listing 4

```c#
[Fact]
public async Task HealthCheckMiddleware_should_set_header_and_return_metrics()
{
  // arrange
  var hostBuilder = new HostBuilder()
  .ConfigureWebHost(webHostBuilder =>
    {
      webHostBuilder.ConfigureServices(services => 
      {
        services.AddScoped<IHealthCheckService, HealthCheckService>();
      });
      webHostBuilder.Configure(applicationBuilder =>
      {
        applicationBuilder.UseMiddleware<HealthCheckMiddleware>();
      });
      webHostBuilder.UseTestServer(); 
  });
 
  var testHost = await hostBuilder.StartAsync();
  var client = testHost.GetTestClient();
 
  // act
  var response = await client.GetAsync("/health");
 
  // assert
  response.EnsureSuccessStatusCode();
  
  Assert.True(response.Headers.Contains("X-Health-Check"));
  
  var body = await response.Content.ReadAsStringAsync();
  Assert.NotEmpty(body);
}
```

Weiter ermöglicht der Testserver mit *SendAsync* eine direkte Manipulation des *HTTPContext*. In Listing 5 wird ebenfalls ein *GET* Request an den Pfad */health* gesendet.

Listing 5

```c#
[Fact]
public async Task HealthCheckMiddleware_should_set_header()
{
  // arrange & act
  using var host = await new HostBuilder()
  .ConfigureWebHost(webBuilder =>
    {
      webBuilder.ConfigureServices(services => 
      {
        services.AddScoped<IHealthCheckService, HealthCheckService>();
      });
      webBuilder.Configure(app =>
      {
        app.UseMiddleware<HealthCheckMiddleware>();
      });
      webBuilder.UseTestServer();
  }).StartAsync();
 
  var server = host.GetTestServer();
  var context = await server.SendAsync(context =>
    {
      context.Request.Method = HttpMethods.Get;
      context.Request.Path = "/health";
  });
 
  // assert
  Assert.True(context.Response.Headers.ContainsKey("X-Health-Check"));
}
```

Selbstverständlich lassen sich mehrere Middlewareklassen der Pipeline hinzufügen und können so im Verbund getestet werden, wenn es sinnvoll sein sollte (Listing 6). Fabrikmustererzeugte Komponenten müssen dabei explizit mit dem DI-Container registriert werden.

Listing 6

```c#
// arrange
var hostBuilder = new HostBuilder()
  .ConfigureWebHost(webHostBuilder =>
    {
      webHostBuilder.ConfigureServices(services =>
        {
          Services.AddScoped<IHealthCheckService, HealthCheckService>();
          services.AddScoped<LoggingMiddleware>();
      });
  
      webHostBuilder.Configure(applicationBuilder =>
        {
          applicationBuilder.UseMiddleware<HealthCheckMiddleware>();
          applicationBuilder.UseMiddleware<LoggingMiddleware>();
          applicationBuilder.Run(async context =>
            {
              await context.Response.WriteAsync("Terminating middleware");
          });
      });
      webHostBuilder.UseTestServer(); 
  });
 
// ..
```

## Verwenden der WebApplicationFactory

Die vorangegangenen Beispiele haben gezeigt, wie sich eine Pipeline für verschiedene Testszenarien zusammenstellen lässt und so Middleware im Verbund getestet werden kann. Möchte man nun die abhängigen Services, DI-Registrationen oder Ähnliches mittesten, kann der Aufwand für den Integrationstest deutlich steigen.

Für derartige Szenarien wird man an der *WebApplicationFactory* Gefallen finden. Diese befindet sich in der Bibliothek Microsoft.AspNetCore.Mvc.Testing und ermöglicht das Testen der gesamten ASP.NET-Core-Anwendung im Speicher. Die *WebApplicationFactory* verwendet intern den *TestServer* und erlaubt das Einbeziehen der echten DI-Registrationen, aller Konfigurationsparameter und natürlich der Pipeline selbst (Listing 7).

Listing 7

```c#
[Fact]
public async Task ExampleApp_should_set_header_and_return_metrics()
{
  // arrange
  var application = new WebApplicationFactory<Program>();
  var client = application.CreateClient();
  
  // act
  var response = await client.GetAsync("/health");
  
  // assert
  response.EnsureSuccessStatusCode();
  Assert.True(response.Headers.Contains("X-Health-Check"));
  
  var body = await response.Content.ReadAsStringAsync();
  Assert.NotEmpty(body);
}
```

Allerdings ist der Test so noch nicht lauffähig. Da seit ASP.NET Core 6 die Programmklasse nicht mehr explizit definiert werden muss, müssen die internen Typen gegenüber der Test-Asssembly noch sichtbar gemacht werden (Listing 8).

Listing 8

```c#
// ExampleApp.csproj
<itemGroup>
  <InternalsVisibleTo Include=”MyTestProject” />
</itemGroup>
 
// Program.cs
using HealthChecks.Middleware;
using HealthChecks.Services;
 
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
 
var app = builder.Build();
app.UseMiddleware<HealthCheckMiddleware>();
 
app.Run();
 
public partial class Program { }
```

## Ersetzen von Abhängigkeiten

Während der Testausführung wird sich die ASP.NET-Core-Anwendung wie im echten, produktiven Betrieb verhalten. Somit gilt es zu beachten, dass während des Tests auch auf etwaige externe APIs und Datenbanken zugegriffen werden kann.

In der Regel möchte man das verhindern und derartige Abhängigkeiten simulieren. Dazu können, wie in Listing 9, mit der Methode *ConfigureTestServices* Abhängigkeiten ersetzt und simulierte Services eingebracht werden.

Listing 9

```c#
private class HealthCheckMock : IHealthCheckService
{
  public bool IsHealthy() => true;
  public string GetMetrics() => "Fake metrics";
}
 
[Fact]
public async Task ExampleApp_should_set_header()
{
  // arrange
  var application = new WebApplicationFactory<Program>()
  .WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
        {
          services.AddSingleton<IHealthCheckService, HealthCheckMock>();
      });
  });
  var client = application.CreateClient();
  
  // act
  var response = await client.GetAsync("/health");
  
  // assert
  response.EnsureSuccessStatusCode();
  Assert.True(response.Headers.Contains("X-Health-Check"));
  
  var body = await response.Content.ReadAsStringAsync();
  Assert.NotEmpty(body);
}
```

## Fazit

Im dritten und letzten Teil dieser Serie haben wir gesehen, wie sich Middleware testen lässt. Wir haben die Health-Check-Middlewarekomponente mit Unit-Tests in Isolation untersucht. Dann haben wir sie mit Hilfe des In-Memory-Test-Servers und ihrer Abhängigkeiten auf die korrekte Funktionalität hin untersucht. Schlussendlich haben wir den gesamten Applikationskontext inklusive DI-Registrationen, Konfiguration und Pipeline miteinbezogen und gesehen, wie sich Abhängigkeiten für Testzwecke ersetzen lassen. Damit sollte dem erfolgreichen Umsetzen eigener Middlewareideen nichts mehr im Wege stehen.