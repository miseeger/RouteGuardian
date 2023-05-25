# ![RouteGuardianLogo_xxs](Assets/RouteGuardianLogo_xxs.png) RouteGuardian
RouteGuardian - Schützt ihre API-Routen mit der RouteGuardian-Middleware oder der RouteGuardian-Policy - stark inspiriert von [F3-Access](https://github.com/xfra35/f3-access).

Der RouteGuardian prüft Regeln, die für eine ressourcenbasierende Autorisierung aufgestellt sind, und gibt je nach Berechtigung den Zugriff für den Anfragenden Benutzer frei. Generell kann das nach der programmatischen Initialisierung einer RouteGuardian-Instanz erfolgen. Die Autorisierung wird dann entweder in einem Basis-Controller vorgenommen oder bei einer Minimal-API, direkt in jedem Endpunkt. Dieser Ansatz ist unter Umständen sehr repititiv und deshalb werden zwei Möglichkeiten angeboten, den RouteGuardian in die Pipeline von ASP.NET einzubinden:

1) als Middleware - `RouteGuardianMiddleware`
2) als Authorization Policy - `RouteGuardianPolicy`

Die RouteGuardian-Middleware und die Policy sind grundlegend auf ein gruppenbasiertes Autorisierungs-Szenario ausgelegt, das sowohl die JWT-Authentifizierung und -Autorisierung als auch die Windows-Authentifizierung und -Autorisierung (über Windows Userrgruppen) unterstützt. Bei der Nutzung der Basisfunktionalität des RouteGuardian können die Prüfrichtlinien je nach Anforderung implementiert werden.

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
- `{num}` - eine beliebige Ziffernfolge
- `{str} ` - eine Folge beliebiger, alphanumerischer Zeichen

Ein Beispiel für den Einsatz von `*` als Wildcard könnte so aussehen:

```c#
var routeGuardian = new RouteGuardian()
	.Allow("*", "/admin*", "ADMIN")               // (1)
    .Allow("*", "/public*, "*")                   // (2)    
	.Allow("*", "/*/edit", "ADMIN")               // (3)
    .Allow("*", "/account/show/{num}", "FINANCE") // (4)
```

Auch hier ist die Default Policy auf `deny` gestellt und somit wirken sich die definierten Regeln wie folgt aus:

1) Alle Routen, die mit`/admin` beginnen, sind nur für die Gruppe `ADMIN` freigegeben.
2) Alle Routen, die mit `/public` beginnen, sind für alle Benutzer freigegeben.
3) Routen, deren Pfad auf `edit` endet, sind ausschließlich für die Gruppe `ADMIN` erlaubt.
4) Die Route `/account/show/`, gefolgt von einer Ziffernfolge (z. b. Kontonummer), ist für die Gruppe `FINANCE` freigebeben.

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

### Mehrere Subjects (berechtige Objekte, wie z. B. Rollen)

### Einlesen der Regeln aus einer Konfigurationsdatei

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

| Methods                                                      | Rückgabe                    | Funktion                                                     |
| ------------------------------------------------------------ | --------------------------- | ------------------------------------------------------------ |
| `GetTokenValidationParameters()`                             | `TokenValidationParameters` | Gibt die in der Konfiguration angegebenen Werte als Objekt vom Typ `TokenValidationParemeters` zurück. |
| `GenerateToken(claims, key, userName, userId, issuer, audience, validForMinutes, algorithm)` | `string`                    | Generiert ein (mit `HmacSha256` verschlüsseltes) JWT. Der Methode muss eine Liste von Claims mitgegeben werden, die mindestens den Claim `rol` hat, der die Rollen des sich authentifizierdenden Benutzers trägt. Zwingend angegeben werden muss auch das Kennwort für die Verschlüsselung. Die folgenden Parameter für die Methode sind wie folgt vorbelegt und müssen nicht angegeben werden und sind per default mit einem Leerstring belegt: `username`,  `userId`, `issuer` , `audience` . `validForMinutes` wird mit 1440 (= 24 Stunden) vorbelegt. Als `algorithm` wird `HmacSha256` vorbelegt. |
| `ValidateToken(authToken)`                                   | true / false                | Prüft das als String angegebenen Token und gibt, gemäß den Einstellungen zur Validierung (s. o.) zurück,  ob es gültig ist. |
| `ReadToken(authToken)`                                       | `JwtSecurityToken`          | Liest das als String angegebene Token, lässt es prüfen und wandelt es in ein `JwtSecurityToken` um. Ist das Token ungültig, wird hier `null` zurückgegeben. |
| `GetSubjectsFromJwtToken(authToken)`                         | `string`                    | Ermittelt die Rollen-Claims aus dem übergebenen und geprüften Token und gibt sie als zusammengesetzten (CSV)-String zurück. |

### WinHelper

Für den Windows-Helper wird keine Konfiguration benötigt. Er funktioniert out-of-the-box.

## JWT-Authentication

## Windows-Authentication

## RouteGuardianMiddleware

## RouteGuardianPolicy



## Lizenzbedingungen

Diese Software wird von [Michael Seeger](https://github.com/miseeger), in Deutschland, mit :heart: entwickelt und betreut. Lizensiert unter [MIT](https://github.com/miseeger/RouteGuardian/blob/main/LICENSE).
