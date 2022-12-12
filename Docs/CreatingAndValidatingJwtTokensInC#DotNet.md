# Creating And Validating JWT Tokens In C# .NET

I’ve recently been using JWT Tokens as my authentication method of choice for my API’s. And with it, I’ve had to do battle with various pieces of documentation on how JWT token authentication and authorization actually work in .NET Core / ASP.NET.

Primarily, there is a lot of documentation on using ASP.NET Identity to handle authentication/authorization. So using the big bloated UserManager and using the packaged attributes like [Authorize] etc. However, I always get to a point where I just need a bit more custom flexibility, that the out of the box components don’t provide. And when it comes to how to ***manually\*** create JWT Tokens and validate them later on, the documentation is a little slim. Infact some guides show you how to manually create the token, but then tell you to use the out of the box components to validate it which creates confusion as to what you’re actually doing. So here’s hoping this article clears some things up!

### Creating JWT Tokens In C# .NET

Let’s first take a look at how to create JWT tokens manually. For our example, we will simply create a service that returns a token as a string. Then however you return that token (header, response body etc) is up to you. I’ll also note in the following examples, we have things like hardcoded “secrets”. I’m doing this for demonstration purposes but quite obviously you will want these to be config driven. You should take the following as a starting point, and then modify it to be production ready.

The code to generate a JWT Token looks like so :

```c#
public string GenerateToken(int userId)
{
	var mySecret = "asdv234234^&%&^%&^hjsdfb2%%%";
	var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

	var myIssuer = "http://mysite.com";
	var myAudience = "http://myaudience.com";

	var tokenHandler = new JwtSecurityTokenHandler();
	var tokenDescriptor = new SecurityTokenDescriptor
	{
		Subject = new ClaimsIdentity(new Claim[]
		{
			new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
		}),
		Expires = DateTime.UtcNow.AddDays(7),
		Issuer = myIssuer,
		Audience = myAudience,
		SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
	};

	var token = tokenHandler.CreateToken(tokenDescriptor);
	return tokenHandler.WriteToken(token);
}
```

Let’s walk through this bit by bit.

I have a security key which is essentially used to “sign” the token on it’s way out. We can verify this signature when we receive the token on the other end to make sure it was created by us. Tokens themselves are actually readable even if you sign them so you should never put sensitive information in them. Signing simply verifies that it was us who created the token and whether it’s been tampered with, but it does not “encrypt” the token.

The Issuer and Audience are funny things because realistically, you probably won’t have a lot of use for them. Issuer is “who” created this token, for example your website, and Audience is “who” the token is supposed to be read by. So a good example might be that when a user logs in, your authentication api (auth.mywebsite.com) would be the issuer, but your general purposes API is the expected audience (api.mywebsite.com). These are actually free text fields so they don’t have to be anything in particular, but later on when we validate the issuer/audience, we will need to know what they are.

We are creating the token for 7 days, but you can set this to anything you want (Or have it not expire it at all), and the rest of the code is just .NET Core specific token writing code. Nothing too specific to what we are doing. Except for claims…

### Explaining Claims

Claims are actually a simple concept, but too many articles go into the “abstract” thought process around them. In really simply terms, a claim is a “fact” stored in the token about the user/person that holds that token. For example, if I log into my own website as an administrator role, then my token might have a “claim” that my role is administrator. Or put into a sentence “Whoever holds this token can claim they are an admin”. That’s really what it boils down to. Just like you could store arbitrary information in a cookie, you can essentially do the same thing inside a JWT Token.

For example, because a claim “type” is simply a free text field, we can do things like :

```c#
Subject = new ClaimsIdentity(new Claim[]
{
	new Claim("UserRole", "Administrator"),
})
```

Notice how we don’t use the “ClaimTypes” static class like we did in the first example, we simply used a string to define the claim name, and then said what the claim value was. You can basically do this for any arbitrary piece of information you want, but again remember, anyone can decode the JWT Token so you should not be storing anything sensitive inside it.

I’ll also note that a great pattern to get into is to store the claim types as static consts/readonly. For example :

```c#
public static readonly string ClaimsRole = "UserRole";

[...]

Subject = new ClaimsIdentity(new Claim[]
{
	new Claim(ClaimsRole, "Administrator"),
})
```

You are probably going to need that ClaimType string in multiple places, so it’s better to set it once and reuse that static variable everywhere.

### Validating A Token

So once you’ve created the token, the next step would be to validate it when a user sends you one. Now personally I like sending it inside a header like x-api-token, but because it’s simply a string, you can send it any which way you like. Because of that, let’s make our example method simply accept a token as a string and validate it.

```c#
public bool ValidateCurrentToken(string token)
{
	var mySecret = "asdv234234^&%&^%&^hjsdfb2%%%";
	var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

	var myIssuer = "http://mysite.com";
	var myAudience = "http://myaudience.com";

	var tokenHandler = new JwtSecurityTokenHandler();
	try
	{
		tokenHandler.ValidateToken(token, new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidIssuer = myIssuer,
			ValidAudience = myAudience,
			IssuerSigningKey = mySecurityKey
		}, out SecurityToken validatedToken);
	}
	catch
	{
		return false;
	}
	return true;
}
```

You’ll notice that I’ve had to copy and paste the security keys, issuer and audience into this method. As always, this would be better in a configuration class rather than being copied and pasted, but it makes the example a little easier to read.

So what’s going on here? It’s pretty simply actually. We create a TokenHandler which is a .NET Core inbuilt class for handling JWT Tokens, we pass it our token as well as our “expected” issuer, audience and our security key and call validate. This validates that the issuer and audience are what we expect, and that the token is signed with the correct key. An exception is thrown if the token is not validated so we can simply catch this and return false.

### Reading Claims

So the final piece of the puzzle is reading claims. This is actually fairly easy assuming we have already validated the token itself.

```c#
public string GetClaim(string token, string claimType)
{
	var tokenHandler = new JwtSecurityTokenHandler();
	var securityToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

	var stringClaimValue = securityToken.Claims.First(claim => claim.Type == claimType).Value;
	return stringClaimValue;
}
```

Read the token, go to the claims list, and find the claim with the matching type (remembering the claimType is simply a freetext string), and return the value.

### What About AddAuthentication/AddJwtBearer?

So you might have read documentation that uses the following code :

```c#
services.AddAuthentication(x =>
{
	x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
	x.TokenValidationParameters = new TokenValidationParameters();
});
```

Or some variation with it that sets up the token validation parameters with signing keys, audiences and issuers. This only works if you are using the default Authorize attribute. These settings are a way for you to configure the inbuilt ASP.NET Core authorization handlers. **It does not set any global settings for JWT Tokens if you are creating/validating them yourself**.

Why do I point this out? I’ve seen people manually validating tokens and *not* validating the signing key. When I ask why they are not validating that the token is signed correctly, they have assumed that if they call AddJwtBearer with various settings that these also pass down anytime you call new JwtSecurityTokenHandler() . They do not!