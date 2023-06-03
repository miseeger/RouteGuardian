# ![RouteGuardianLogo_xxs](Assets/RouteGuardianLogo_xxs.png) RouteGuardian
[![NuGet](https://img.shields.io/nuget/v/RouteGuardian.svg)](https://www.nuget.org/packages/RouteGuardian) [![lic](https://img.shields.io/badge/license-MIT-blue)](https://github.com/miseeger/RouteGuardian/blob/main/LICENSE)

RouteGuardian schützt API-Routen mit der RouteGuardian-Middleware oder der RouteGuardian-Policy - stark inspiriert von [F3-Access](https://github.com/xfra35/f3-access).

Der RouteGuardian prüft Regeln, die für eine ressourcenbasierende Autorisierung aufgestellt sind, und gibt je nach Berechtigung den Zugriff für den Anfragenden Benutzer frei. Generell kann das nach der programmatischen Initialisierung einer RouteGuardian-Instanz erfolgen. Die Autorisierung wird dann entweder in einem Basis-Controller vorgenommen oder bei einer Minimal-API, direkt in jedem Endpunkt. Dieser Ansatz ist unter Umständen sehr repititiv und deshalb werden zwei Möglichkeiten angeboten, den RouteGuardian in die Pipeline von ASP.NET einzubinden:

1) als Middleware - `RouteGuardianMiddleware`
2) als Authorization-Policy - `RouteGuardianPolicy`

Die RouteGuardian-Middleware und die Policy sind grundlegend auf ein gruppenbasiertes Authorisierungs-Szenario ausgelegt, das sowohl die JWT-Authentifizierung und -Autorisierung als auch die Windows-Authentifizierung und -Autorisierung (über Windows Usergruppen) unterstützt. Bei der Nutzung der Basisfunktionalität des RouteGuardian können die Prüfrichtlinien je nach Anforderung implementiert werden.

Zusätzlich stellt der RouteGuardian einen JwtHelper für die Webtoken-Verarbeitung  und eine WinHelper zur Verarbeitung von AD-Gruppenberechtigungen aus der Windows Authentication bereit. Letzterer implementiert auch einen GroupsCache für die WinAuth.

## Generelle Nutzung

Der RouteGuardian verfügt über eine generelle Richtlinie (Policy), die als Standard entweder den Zugriff auf API-Endpunkte (Ressourcen) freigibt (`deny`) oder zulässt (`allow`). Wird der RouteGuardian instanziiert, steht die Default Policy auf `deny`. Das bedeutet, dass alle Routen generell gesperrt sind, wenn sie nicht explizit freigegeben: Need-To-Know-Prinzip.

```c#
var routeGuardian = new RouteGuardian();
```

Im Gegenzug kann man die Policy entsprechend umkehren, wenn generell alle Ressourcen bis auf ein paar frei zugänglich sein sollen:

```c#
var routeGuardian = new RouteGuardian()
    .DefaultPolicy(GuardPolicy.Allow);
```

### Definition der Zugriffsregeln

Der Zugriff auf eine Route wird entweder erlaubt oder verweigert. Dabei kann die (oder mehrere) HTTP-Methode(n) mit gangeben werden. Für alle Methoden wird ein Wildcard (`*`) angegeben. Weiterhin wird definiert, für welche Benutzer (oder Gruppen, etc.) die Regel aufgestellt ist.

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/admin", "ADMIN|PROD")   // (1)
    .Deny("*", "/admin/part2", "*")       // (2)
    .Allow("*", "/admin/part2", "ADMIN"); // (3)
```

Die oben definierten Regeln (Default Policy = `deny`), wirken sich wie folgt aus:

1) Benutzer in den Rollen `ADMIN ` oder `PROD` haben Zugriff auf die Route `/admin` (nur bis da hin!) Alle HTTP-Methoden sind erlaubt.
2) Für alle Rollen ist die Route `/admin/part2` gesperrt, und zwar ebenso für all HTTP-Methoden. Das ist bei der Default Policy `deny` implizit der Fall.
3) Die in 2. gesetzte Regel wird für die Rolle `ADMIN` wieder aufgehoben und somit hier die Route `/admin/part2` für alle HTTP-Methoden freigebeben.

> **Wichtig!** Alle Prüfungen der Zugriffsregeln erfolgen ohne Beachtung der Groß-/Kleinschreibung. Intern werden alle Routen in Kleinbuchstaben umgesetzt. Verben und die Rollen/Subjects werden als Großbuchstaben behandelt. Damit soll ein Minimum an Fehlertoleranz erreicht werden. Selbstverständlich müssen die entsprechenden Angaben (Verben, Routes und Subjects) korrekt geschrieben werden.

#### Wildcards

Die Routen, für die die Zugriffsregeln festgelegt werden, können Wildcards enthalten. Dabei werden folgende Wildcards unterstützt:

- `*` - alle Routen-Fragmente, die an der Stelle des Asterisk stehen.
- `{int}` - eine Ganzzahl, mit Vorzeichen (RegEx Pattern: `[+-]?(?<!\.)\b[0-9]+\b(?!\.[0-9]`)
- `{dec}` - eine Dezimalzahl, mit Vorzeichen und Punkt als Dezimaltrennzeichen (RegEx Pattern: `[+-]?(?:\d*\.)?\d+`)
- `{str} ` - eine Folge beliebiger, alphanumerischer Zeichen. Als Sonderzeichen ist der Bindestricht, der Unterstricht und auch das Leerzeichen erlaubt (RegEx Pattern: `[a-zA-Z0-9_-]+`)
- {guid} - eine GUID (RegEx Pattern: `[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?`)

Ein Beispiel für den Einsatz von `*` als Wildcard könnte so aussehen:

```c#
var routeGuardian = new RouteGuardian()
	.Allow("*", "/admin*", "ADMIN")                 // (1)
    .Allow("*", "/public*, "*")                     // (2)    
	.Allow("*", "/*/edit", "ADMIN")                 // (3)
```

Auch hier ist die Default Policy auf `deny` gestellt und somit wirken sich die definierten Regeln wie folgt aus:

1) Alle Routen, die mit`/admin` beginnen, sind nur für die Gruppe `ADMIN` freigegeben.
2) Alle Routen, die mit `/public` beginnen, sind für alle Benutzer freigegeben.
3) Routen, deren Pfad auf `edit` endet, sind ausschließlich für die Gruppe `ADMIN` erlaubt.

Hier einige Beispiele zur Verwendung der "konkreten" Wildcards, die auch eine Prüfung auf Korrektheit der für den Platzhalter angegebenen Werte, beinhaltet

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/products/{guid}", "*")            
    .Allow("*", "/products/{guid}/load/{dec}", "*")
    .Allow("*", "/products/report/page/{int}", "*")
    .Allow("*", "/products/report/{str}", "*"); 
```

### Priorität der Pfade/Routen

Die Priorität der defnierten und zu prüfenden Routen erfolgt von der spezifischsten Route zur am wenigsten spezifischen Route. Das bedeutet: Routen mit Wildcards werden nach den spezifischen Routen behandelt:

Diese Routen 

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/admin*", "ADMIN")        
    .Allow("*", "/admin/blog/foo", "ADMIN")
    .Allow("*", "/admin/blog", "ADMIN")        
    .Allow("*", "/admin/blog/foo/bar","ADMIN")
    .Allow("*", "/admin/blog/*/bar","ADMIN")
```

... würden mit folgender Priorität behandelt:

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/admin/blog/foo/bar","ADMIN")
    .Allow("*", "/admin/blog/*/bar","ADMIN")    
    .Allow("*", "/admin/blog/foo", "ADMIN")
    .Allow("*", "/admin/blog", "ADMIN")        
    .Allow("*", "/admin*", "ADMIN")        
```

**Wichtig:** Es greift die erste Regel, zu der der angefragte Pfad passt.

### Mehrere HTTP-Verben

Ausgewählte HTTP-Verben für eine Route können mit einer Regel versehen werden. Dabei müssen diese nicht zwingend als eine Regel definiert werden. Durch die Trennung mit der Pipe (`|`) als Trennzeichen werden mehrere Verben für eine Regel hinterlegt:

```c#
var routeGuardian = new RouteGuardian()
    .Clear()
    .DefaultPolicy(GuardPolicy.Allow)
    .Deny("POST|PUT|DELETE", "/blog/Entry", "*") // (1)
    .Allow("*", "/blog/entry", "ADMIN");         // (2)
```

1. Für alle Subjects wird HTTP `POST`, `PUT` und `DELETE` verweigert.
2. Nur die (Rolle) `ADMIN` hat Zugriff auf alle HTTP-Verben des Pfads `/blog/entry`.

### Mehrere Subjects (berechtige Objekte, wie z. B. Rollen)

So wie es möglich ist, mehrere HTTP-Verben für eine Regel zu hinterlegen, ist es auch möglich, mehrere Subjects für eine Regel zu berücksichtigen:

```c#
var routeGuardian = new RouteGuardian()
    .Clear()
    .DefaultPolicy(GuardPolicy.Allow)
    .Deny("POST|PUT|DELETE", "/blog/entry", "*") // (1)
    .Allow("*", "/blog/entry", "ADMIN");         // (2)
```

​	Erklärung: s. o.

Im Anwendungsfall der Prüfung eines Zugriffs auf eine Route (mit `IsGranted()`, ist es ebenso möglich, mehrere Subjects zur Prüfung heranziehen, wenn der anfragendes Benutzer z. B. mehrere Berechtigungsrollen besitzt. Hier ein Beispiel aus den Tests zum RouteGuardian, die diese Anwendung klar machen sollen*:

```c#
Assert.IsTrue(
    routeGuardian.IsGranted("GET", "/blog/entry", "Client|CUSTOMER")
    && !routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|customer")
    && routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|admin")
);
```

​	* Alle Angaben hier werden ungeachtet der Groß-/Kleinschreibung behandelt. 

### Einlesen der Regeln aus einer Konfigurationsdatei

Eine programmatisch festgelegtes Regel-Set für den Zugriff auf bestimmte Routen kann unter Umständen nicht die beste Lösung sein, weil Änderungen eine Neukompilierung und ein erneutes Publishing des Projekts bedingt. Eine Abhilfe schafft hier die Möglichkeit, eine Konfigurationsdatei mit der Default-Policy und den Regeln für die Endpunkte zu hinterlegen. Diese Datei muss den Namen `access.json` tragen und im Hauptverzeichnis der Anwendung liegen. Sie hat als Minimalkonfiguration folgende Ausprägung:

```json
{
  "default": "deny", // (1)
  "rules": []        // (2)
}
```

1. Alle Routen werden verboten (Need-To-Know Prinzip)
2. Es werden keine Regeln definiert.

Die Regeln werden, ähnlich wie bei der programmatischen Konfiguration, angegeben. Dabei werden die Segmente (Policy, HTTP-Verb, Route, Subjects) durch **ein(!)** Leerzeichen getrennt.

```json
{
  "default": "deny",
  "rules": [
    "allow GET /foo/bar/admin ADMIN|PROD",
    "deny POST /foo/bar/admin *",
    "allow * /admin ADMIN|PROD",
    "deny * /admin/part2 *",
    "allow GET /api/test/test ADMIN",
    "deny GET /api/test/xyz ADMIN"
  ] 
}
```

Auch hier gilt: Bei allen Angaben wird die Groß-/Kleinschreibung ignoriert.

> Das Vorhandensein der Datei `access.json` schließt nicht aus, dass programmatisch je nach Anforderung doch fixe Regeln zum RouteGuardian hinzugefügt werden.

## Helper

### JwtHelper

#### Konfiguration

Beim Instanzieren des `JwtHelper` wird eine Konfiguration benötigt, die wahlweise in der `appsettings.json` global oder in den Umgebungs-Settings (per Umgebung) geregelt wird. Die benötigten Angaben werden für das Handling von JSON Web Tokens benötigt und sind für die JwtAuthentication wie folgt anzugeben:

```json
/// appsettings[.Development|.Production].json
{
    ...
    "RouteGuardian": {
        "JwtAuthentication": {
            "ApiSecretEnVarName": "JwtDevSecret",
            "ValidateIssuer": "true",
            "ValidateAudience": "true",
            "ValidateIssuerSigningKey": "true",
            "ValidateLifetime": "false",
            "ValidIssuer": "RouteGuardian",
            "ValidAudience": "RouteGuardianTests"
        }
    },
    ...
}
```

| Property                   | Werte               | Bedeutung                                                    |
| -------------------------- | ------------------- | ------------------------------------------------------------ |
| `ApiSecretEnvarName`       | beliebig (`string`) | Der Name der Environment-Variablen, unter der der Secret Key für die Verschlüsselung des JWT benutzt wird. |
| `ValidateIssuer`           | true / false        | Regelt, ob beim Validieren des Tokens der Herausgeber geprüft wird. |
| `ValidateAudience`         | true / false        | Bestimmt, ob beim Validieren des Tokens der Anfragende geprüft wird. |
| `ValidateIssuerSigningKey` | true / false        | Regelt, ob beim Validieren des Tokens der Secret Key geprüft wird |
| `ValidateLifetime`         | true / false        | Legt fest, ob beim Validieren des Tokens der Gültigkeitszeitraum (default: 1440 Minuten) geprüft wird. |
| `ValidIssuer`              | beliebig (String)   | Der Name des Herausgebers, für den der Token gültig ist.     |
| `ValidAudience`            | beliebig            | Der Name des Anfragenden, für den der Token gültig ist.      |

#### Interface

| Property   | Typ                     | Rückgabewert                                                 |
| ---------- | ----------------------- | ------------------------------------------------------------ |
| `Settings` | `IConfigurationSection` | Enthält die Werte der zuvor beschriebenen JWT-Konfiguration  |
| `Secret`   | `string`                | Das in einer Umgebungsvariablen im System gespeicherte Verschlüsselungs-Kennwort für JWTs |

| Methode                                                      | Rückgabe                    | Funktion                                                     |
| ------------------------------------------------------------ | --------------------------- | ------------------------------------------------------------ |
| `GetTokenValidationParameters()`                             | `TokenValidationParameters` | Gibt die in der Konfiguration angegebenen Werte als Objekt vom Typ `TokenValidationParemeters` zurück. |
| `GenerateToken(claims, key, userName, userId, issuer, audience, validForMinutes, algorithm)` | `string`                    | Generiert ein (mit `HmacSha256` verschlüsseltes) JWT. Der Methode muss eine Liste von Claims mitgegeben werden, die mindestens den Claim `rol` hat, der die Rollen des sich authentifizierdenden Benutzers trägt. Zwingend angegeben werden muss auch das Kennwort für die Verschlüsselung. Die folgenden Parameter für die Methode sind wie folgt vorbelegt und müssen nicht angegeben werden und sind per default mit einem Leerstring belegt: `username`,  `userId`, `issuer` , `audience` . `validForMinutes` wird mit 1440 (= 24 Stunden) vorbelegt. Als `algorithm` wird `HmacSha256` vorbelegt. |
| `ValidateToken(authToken)`                                   | true / false                | Prüft das als String angegebenen Token und gibt, gemäß den Einstellungen zur Validierung (s. o.) zurück,  ob es gültig ist. |
| `ReadToken(authToken)`                                       | `JwtSecurityToken?`         | Liest das als String angegebene Token, lässt es prüfen und wandelt es in ein `JwtSecurityToken` um. Ist das Token ungültig, wird hier `null` zurückgegeben. |
| `GetSubjectsFromJwtToken(authToken)`                         | `string`                    | Ermittelt die Rollen-Claims aus dem übergebenen und geprüften Token und gibt sie als zusammengesetzten (CSV)-String zurück. |
| GetTokenFromContext(context)                                 | `string`                    | Liest das JWT-Token aus dem Authorization-Header des Request (im `HttpContext`) aus. |
| GetTokenClaimsFromContext(context)                           | `List<Claim>?`              | Liest alle Claims des JWT-Token aus dem Authorization-Header des Request (im `HttpContext`) aus. Wurde ein ungültiger Token festgestellt, wird `null` zurückgeliefert. |
| GetTokenClaimValueFromContext(context, claimType)            | `string?`                   | Liefert den Wert eines Claims (ermittelt über den angegebenen `claimType`) aus den Claims, die aus dem JWT-Token im Authorization-Header des Request ermittelt werden. Ist der Token ungültig oder der ClaimType nicht gesetzt/vorhanden, dann wird `null` zurückgegeben. |

### WinHelper

Der WinHelper kümmert sich um die Ermittlung der dem über Windows Single-Sign-On authentifizierten Benutzer zugewiesenen Gruppen aus dem Active Directory. Diese können über Hilfsmethoden in Klartext gewandelt werden und als RouteGuardian-Subjects zurückgeliefert werden. Dieser Methodik bedient sich die Middleware des RouteGuardian und die RouteGuardian-Policy für API-Endpunkte.

Da der Prozess der Ermittlung von AD-Benutzergruppen und deren Umwandlung in Klartext kostbare Zeit kostet, werden die einmal ermittelten Benutzergruppen pro Benutzer gecashed und mit einem Hashcode versehen. Dieser Hashcode wird aus den GUIDs der AD-Gruppen errechnet, noch bevor die Übersetzung in Klartext geschieht. So wird dann nur bei Änderungen in den AD-Gruppen eines Benutzers eine Neu-Übersetzung notwendig.

#### Konfiguration

Für den Windows-Helper wird keine Konfiguration benötigt. Er funktioniert out-of-the-box.

#### Interface

| Methode                               | Rückgabe | Funktion                                                     |
| ------------------------------------- | -------- | ------------------------------------------------------------ |
| GetWinUserGroupsHash(identity)        | `string` | Ermittelt den Hashwert aller AD-Grupen des authentifizierten Benutzers (`WindowsIdentity`). Der Hash wird als MD5-Hash geliefert. |
| GetSubjectsFromWinUserGroups(context) | `string` | Liefert alle RouteGuardian-Subjects (Klartext der AD-Gruppen) eines authentifizierten Benutzers aus dem übergebenen `HttpContext`. Diese Methode nutzt den bereits beschriebenen Windows-User Groups-Cache. |
| ClearWinUserGroupsCache()             | keine    | Löscht den `WinUserGroupsCache`.                             |

## JWT-Authentication

RouteGuardian liefert eine vorgefertigte Extension Method für die `IServiceCollection`, die während der Service-Konfiguration in `Program.cs` dafür sorgt, dass die Anwendung JWT-Authentication nutzen kann und der JWTHelpter (als `IJwtHelper`) für die Depencency-Injection bereitgestellt wird.

Die Konfiguration ist wie folgt:

`Program.cs`

```c#
using RouteGuardian.Extension;

// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

...
    
// ----- Authentication and Authorization -------------------------------------
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// ----- Register and configure JWT-Authenticaiton ----------------------------
builder.Services.AddJwtAuthentication(builder.Configuration);

...
```

Wichtig ist hier, dass sowohl die Authentication und Authorization eingebunden wird und für die JwtAuthentication im Speziellen die Configuration (`appsettings.json`) noch mit übergeben wird, aus der die notwendigen Einstellungen für die JWT-Authentifizierung entnommen werden (siehe weiter oben).

## Windows-Authentication

Mit der Windows-Authentication verhält es sich ähnlich wie mit der JWT-Authentication. Auch sie wird über eine Extension Method bereitgestellt, die den WinHelper (als `IWinHelper`) für die Dependency-Injection bereitstellt.

Die Konfiguration in  `Programm.cs` ist wie folgt:

```c#
using RouteGuardian.Extension;

// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

...
    
// ----- Authentication and Authorization -------------------------------------
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// ----- Register and configure Windows-Authenticaiton ------------------------
builder.Services.AddWindowsAuthentication(builder.Configuration);

...
```

## RouteGuardianPolicy

Die RouteGuardianPolicy ist eine Policy mit dem Namen "RougeGuardian", welche zur Prüfung des authorisierten Zugriffs auf einen API-Endpunkt genutzt wird und dabei die Regeln des RouteGuardian prüft.

Registriert wird die Policy auch in der `Program.cs`, wie z. B. so:

```c#
using RouteGuardian.Extension;

// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

...
    
// ----- Register and configure JWT-Authenticaiton ----------------------------
builder.Services.AddWindowsAuthentication(builder.Configuration);
builder.Services.AddRouteGuarianPolicy("access.json");
...
```

Die einzige Information, die die Policy zur Konfiguration benötigt, ist der Pfad der Datei mit den zu nutzenden Zugriffsregeln.

> Wird die RouteGuardianPolicy in einer Anwendung verwendet, benötigt man keine RouteGuardianMiddleware.

#### Verwendung der RouteGuardianPolicy

Dieses kurze Code-Beispiel zeigt die Verwendung der RouteGuardianPolicy an einem sehr einfachen Minimal-API-Endpunkt, an dem tatsächlich nur die Methode `RequireAuthorization` mit der Angabe der registrierten Policy "RouteGuardian" angehängt wird.

```c#
app.MapGet("/helloworld", () => "Hello World!")
    .RequireAuthorization("RouteGuardian");
```

In einem Controller im MVC-Stil wird der Endpunkt über das `[Authorize]`-Attribut gesichert:

```c#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyApi.Controllers;

// [Authorize(Policy = "RouteGuardian")]                    // (1)
public class MyController : Controller
{
    [Authorize(Policy = "RouteGuardian")]                   // (2) 
    public IActionResult Hello() => "Hello secret World!";
}
```

1. Mit dem Attribut können entweder alle Controller-Endpunkte ...
2. ... oder nur gezielte Endpunkte geschützt werden

## RouteGuardianMiddleware

Die RouteGuardian Middleware wird in die Pipeline zwischen Authentication- und Authorization-Middleware und noch vor der Mapping-Middleware für die Controller gesetzt. Diese Konfiguration wird wie bei der Policy in der `Program.cs` vorgenommen.

Die Einbindung geschieht z. B. wie folgt:

```c#
using RouteGuardian.Extension;
// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

... 

// ===== Pipeline (Middleware) ================================================
var app = builder.Build();

...

app.UseAuthentication();
app.UseAuthorization();

// Einbindung der RouteGuardian Middleware:
app.UseRouteGuardian("/api"); 

app.MapControllers();

app.Run();
```

Die einzige Information, die die RouteGuardian Middleware für die Konfiguration benötigt, ist der Basispfad der zu schützenden Endpunkte, hier "/api". Die zu berücksichtigenden Regeln werden beim Starten der Anwendung aus der Definitionsdatei mit dem Namen `access.json`  gelesen (siehe weiter oben).

> Wird die RouteGuardianMiddleware in einer Anwendung verwendet, benötigt man keine RouteGuardianPolicy.

## Extras

Die RouteGuardian-Bibliothek bringt noch ein paar Goodies mit, die nicht unbedingt in den Kontext der Absicherung von API-Routen gehört, aber nützlich für eine (Web-)Anwendung sein könnten.

### GlobalExceptionHandlerMiddleware

Die `GlobalExceptionHandlerMiddleware` wird ganz an den Anfang der Request-Pipeline gesetzt und fängt globale, nicht behandelte Exceptions ab, loggt diese und gibt eine 500er HTTP-Response mit dem Text der Exception-Message zurück, wenn dies gewünscht ist. Wenn nicht, dann wird eine allgemeingültige Meldung zurückgegeben.

```c#
...

// ===== Pipeline (Middleware) ================================================
var app = builder.Build();

app.UseGlobalExceptionHandler(true); // (1)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

...

app.Run();
```

1. Der globale Exceptionhandler wird als erstes Glied in die Request-Pipeline eingebunden und die qualifizierte Fehlermeldung eingeschaltet. Ohne Angabe/Parameter ist die Rückgabe von Fehlermeldungen (als Default) unterbunden.

### StringExtension(s)

Die einzige String-Extension in dieser Bibliothek bietet die Errechnung eines MD5-Hashstrings. Sie erweitert den String-Typ um die Methode `ComputeMd5(string)`. Anwendung findet sie im RouteGuardian beim "hashen" der AD-Berechtigungsgruppen für den `WinUserGroupsCache`.

## Lizenzbedingungen

Diese Software wird von [Michael Seeger](https://github.com/miseeger), in Deutschland, mit :heart: entwickelt und betreut. Lizensiert unter [MIT](https://github.com/miseeger/RouteGuardian/blob/main/LICENSE).
