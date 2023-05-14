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

### Definieren von Zugriffsregeln

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

### Mehrere Subjects (berechtige Objekte)

## Die RouteGuardianMiddleware



## Die RouteGuardianPolicy



## Lizenzbedingungen

Diese Software wird von [Michael Seeger](https://github.com/miseeger), in Deutschland, mit :heart: entwickelt und betreut. Lizensiert unter [MIT](https://github.com/miseeger/NBean/blob/main/LICENSE.txt).
