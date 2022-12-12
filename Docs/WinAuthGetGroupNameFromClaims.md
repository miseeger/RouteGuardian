Question:

I have a asp.net5 project setup to use windows authentication. When I set a break point and look at the User, I see that there is a Claims array that contains Group SID's. How do I get the actual group name from the claims?

Answer:

```cs
private string[] GetGroups1()
{
    var groups = new List<string>();

    var wi = (WindowsIdentity)User.Identity;
    if (wi.Groups != null)
    foreach (var group in wi.Groups)
    {
        try
        {                                
            groups.Add(group.Translate(typeof(NTAccount)).ToString());
        } catch (Exception) {
        // ignored
        }
    }

    groups.Sort(); // optional
    return groups.ToArray();
}
```