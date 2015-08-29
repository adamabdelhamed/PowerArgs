###Binary
PowerArgs is available at the [Official NuGet Gallery](http://nuget.org/packages/PowerArgs).

Reference information for the entire API surface of the latest version is available [here](http://adamabdelhamed2.blob.core.windows.net/powerargsdocs/2.7.0.0/html/classes.html).

###Overview

PowerArgs converts command line arguments into .NET objects that are easy to program against.  It also provides a ton of additional, optional capabilities that you can try such as argument validation, auto generated usage, tab completion, and plenty of extensibility.

It can also orchestrate the execution of your program. Giving you the following benefits:

- Consistent and natural user error handling
- Invoking the correct code based on an action (e.g. 'git push' vs. 'git pull')
- Focus on writing your code

Here's a simple example that just uses the parsing capabilities of PowerArgs.  The command line arguments are parsed, but you still have to handle exceptions and ultimately do something with the result.

```cs    
// A class that describes the command line arguments for this program
public class MyArgs
{
    // This argument is required and if not specified the user will 
    // be prompted.
    [ArgRequired(PromptIfMissing=true)]
    public string StringArg { get; set; }

    // This argument is not required, but if specified must be >= 0 and <= 60
    [ArgRange(0,60)]
    public int IntArg {get;set; }
}

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var parsed = Args.Parse<MyArgs>(args);
            Console.WriteLine("You entered string '{0}' and int '{1}'", parsed.StringArg, parsed.IntArg);
        }
        catch (ArgException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<MyArgs>());
        }
    }
}
```

Here's the same example that lets PowerArgs do a little more for you.  The application logic is factored out of the Program class and user exceptions are handled automatically.  The way exceptions are handled is that any exception deriving from ArgException will be treated as user error.  PowerArgs' built in validation system always throws these type of exceptions when a validation error occurs. PowerArgs will display the message as well as auto-generated usage documentation for your program.  All other exceptions will still bubble up and need to be handled by your code.

```cs    
// A class that describes the command line arguments for this program
[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public class MyArgs
{
    // This argument is required and if not specified the user will 
    // be prompted.
    [ArgRequired(PromptIfMissing=true)]
    public string StringArg { get; set; }

    // This argument is not required, but if specified must be >= 0 and <= 60
    [ArgRange(0,60)]
    public int IntArg {get;set; }

	// This non-static Main method will be called and it will be able to access the parsed and populated instance level properties.
	public void Main()
	{
	    Console.WriteLine("You entered string '{0}' and int '{1}'", this.StringArg, this.IntArg);
	}
}

class Program
{
    static void Main(string[] args)
    {
        Args.InvokeMain<MyArgs>(args);
    }
}
```

Then there are more complicated programs that support multiple actions.  For example, the 'git' program that we all use supports several actions such as 'push' and 'pull'.  As a simpler example, let's 
say you wanted to build a calculator program that has 4 actions; add, subtract, multiply, and divide.  Here's how PowerArgs makes that easy.

```cs
[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public class CalculatorProgram
{
    [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
    public bool Help { get; set; }

    [ArgActionMethod, ArgDescription("Adds the two operands")]
    public void Add(TwoOperandArgs args)
    {
        Console.WriteLine(args.Value1 + args.Value2);
    }

    [ArgActionMethod, ArgDescription("Subtracts the two operands")]
    public void Subtract(TwoOperandArgs args)
    {
        Console.WriteLine(args.Value1 - args.Value2);
    }

    [ArgActionMethod, ArgDescription("Multiplies the two operands")]
    public void Multiply(TwoOperandArgs args)
    {
        Console.WriteLine(args.Value1 * args.Value2);
    }

    [ArgActionMethod, ArgDescription("Divides the two operands")]
    public void Divide(TwoOperandArgs args)
    {
        Console.WriteLine(args.Value1 / args.Value2);
    }
}

public class TwoOperandArgs
{
    [ArgRequired, ArgDescription("The first operand to process"), ArgPosition(1)]
    public double Value1 { get; set; }
    [ArgRequired, ArgDescription("The second operand to process"), ArgPosition(2)]
    public double Value2 { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        Args.InvokeAction<CalculatorProgram>(args);
    }
}
```

Again, the Main method in your program class is just one line of code. PowerArgs will automatically call the right method in the CalculatorProgram class based on the first argument passed on the command line.  If the user doesn't specify a valid action then they get a friendly error.  If different actions take different arguments then PowerArgs will handle the validation on a per action basis, just as you would expect.

Here are some valid ways that an end user could call this program:

* `Calculator.exe add -Value1 1 -Value2 5` outputs '6'
* `Calculator.exe multiply /Value1:2 /Value2:5` outputs '10'
* `Calculator.exe add 1 4` outputs '5' - Since the [ArgPosition] attribute is specified on the Value1 and Value1 properties, PowerArgs knows how to map these arguments.

If you wanted to, your action method could accept loose parameters in each action method.  I find this is useful for small, simple programs where the input parameters don't need to be reused across many actions. 

```cs
[ArgActionMethod, ArgDescription("Divides the two operands")]
public void Add(
	[ArgRequired][ArgDescription("The first value to add"), ArgPosition(1)] double value1, 
	[ArgRequired][ArgDescription("The second value to add"), ArgPosition(2)] double value2)
{
    Console.WriteLine(value1 / value2);
}
```

You can't mix and match though.  An action method needs to be formatted in one of three ways:

- No parameters - Meaning the action takes no additional arguments except for the action name (i.e. '> myprogram.exe myaction').
- A single parameter of a complex type whose own properties describe the action's arguments, validation, and other metadata. The first calculator example used this pattern.
- One or more 'loose' parameters that are individually revivable, meaning that one command line parameter maps to one property in your class. The second calculator example showed a variation of the Add method that uses this pattern.

###Metadata Attributes 

These attributes can be specified on argument properties. PowerArgs uses this metadata to influence how the parser behaves.

* `[ArgPosition(0)]` This argument can be specified by position (no need for -propName)
* `[ArgShortcut("n")]` Lets the user specify -n
* `[ArgDescription("Description of the argument")]`
* `[ArgExample("example text", "Example description")]`
* `[HelpHook]` Put this on a boolean property and when the user specifies that boolean. PowerArgs will display the help info and stop processing any additional work. If the user is in the context of an action (e.g. myprogram myaction -help) then help is shown for the action in context only.
* `[ArgDefaultValue("SomeDefault")]` Specify the default value
* `[ArgIgnore]` Don't populate this property as an arg
* `[StickyArg]` Use the last used value if not specified.  This is preserved across sessions.  Data is stored in <User>/AppData/Roaming/PowerArgs by default.
* `[Query(typeof(MyDataSource))]` Easily query a data source (see documentation below).
* `[TabCompletion]` Enable tab completion for parameter names (see documentation below)
    
###Validator Attributes

These attributes can be specified on argument properties.  You can create custom validators by implementing classes that derive from ArgValidator.

* `[ArgRequired(PromptIfMissing=bool)]` This argument is required.  There is also support for conditionally being required.
* `[ArgExistingFile]` The value must match the path to an existing file
* `[ArgExistingDirectory]` The value must match the path to an existing directory
* `[ArgRange(from, to)]` The value must be a numeric value in the given range.
* `[ArgRegex("MyRegex")]` Apply a regular expression validation rule
* `[UsPhoneNumber]` A good example of how to create a reuable, custom validator.

###Custom Revivers

Revivers are used to convert command line strings into their proper .NET types.  By default, many of the simple types such as int, DateTime, Guid, string, char,  and bool are supported.

If you need to support a different type or want to support custom syntax to populate a complex object then you can create a custom reviver.

This example converts strings in the format "x,y" into a Point object that has properties "X" and "Y".

```cs
public class CustomReviverExample
{
    // By default, PowerArgs does not know what a 'Point' is.  So it will 
    // automatically search your assembly for arg revivers that meet the 
    // following criteria: 
    //
    //    - Have an [ArgReviver] attribute
    //    - Are a public, static method
    //    - Accepts exactly two string parameters
    //    - The return value matches the type that is needed

    public Point Point { get; set; }

    // This ArgReviver matches the criteria for a "Point" reviver
    // so it will be called when PowerArgs finds any Point argument.
    //
    // ArgRevivers should throw ArgException with a friendly message
    // if the string could not be revived due to user error.
  
    [ArgReviver]
    public static Point Revive(string key, string val)
    {
        var match = Regex.Match(val, @"(\d*),(\d*)");
        if (match.Success == false)
        {
            throw new ArgException("Not a valid point: " + val);
        }
        else
        {
            Point ret = new Point();
            ret.X = int.Parse(match.Groups[1].Value);
            ret.Y = int.Parse(match.Groups[2].Value);
            return ret;
        }
    }
}
```

###Generate usage documentation from templates (built in or custom)

PowerArgs has always provided auto-generated usage documentation via the ArgUsage class.  However, the format was hard coded, and gave very little flexibility in terms of the output format. With the latest release of PowerArgs usage documentation can be fully customized via templates.  A template is just a piece of text that represents the documentation structure you want along with some placeholders that will be replaced with the actual information about your command line application.  There are built in templates designed for the console and a web browser, and you can also create your own.  

In its latest release, PowerArgs adds a new method called ArgUsage.GenerateUsageFromTemplate().  The method has several overloads, most of which are documented via XML intellisense comments.  The part that needs a little more explanation is the template format.  To start, let's talk about the built in templates.

The first one, the default, is designed to create general purpose command line usage documentation that is similar to the older usage documentation that PowerArgs generated.  You can see what that template looks like [here](https://github.com/adamabdelhamed/PowerArgs/blob/master/PowerArgs/Resources/DefaultConsoleUsageTemplate.txt).


The second one is designed to create documentation that looks good in a browser.  You can see what that template looks like [here](https://github.com/adamabdelhamed/PowerArgs/blob/master/PowerArgs/Resources/DefaultBrowserUsageTemplate.html).  

[Here is an example of what the built in browser usage looks like](http://adamabdelhamed2.blob.core.windows.net/powerargs/Samples/BrowserUsage.html).


You can create your own templates from scratch, or modify these default templates to suit your needs.  The templating engine was built specifically for PowerArgs.  It has quite a bit of functionality and extensibility, but for now I'm only going to document the basics.

Most of you probably use the class and attributes model when using PowerArgs, but under the hood there's a pretty extensive object model that gets generated from the classes you build.  That model is what is bound to the template.   If you're not familiar with the object model you can explore the code [here](https://github.com/adamabdelhamed/PowerArgs/blob/master/PowerArgs/ArgDefinition/CommandLineArgumentsDefinition.cs).

You can see from the built in templates that there is placeholder syntax that lets you insert information from the model into template.  For example, if your program is called 'myprogram' then the following text in the template would be replaced with 'myprogram'.
    
    {{ExeName !}} is the best
    // outputs - 'myprogram is the best'
    
Additionally, you can add a parameter to the replacement tag that indicates the color to use when printed on the command line as a ConsoleString. You can use any [ConsoleColor](http://msdn.microsoft.com/en-us/library/system.consolecolor(v=vs.110).aspx) as a parameter.

    {{ExeName Cyan !}}     

You can also choose to conditionally include portions of a template based on a property.  Here's an example from the default template:

    {{if HasActions}}Global options!{{if}}{{ifnot HasActions}}Options!{{ifnot}}:

In this case, if the HasActions property on the CommandLineArgumentsDefinition object is true then the usage will output 'Global options'.  Otherwise it will output 'Options'.  This flexibility is important since some command line programs have only simple options while others expose multiple commands within the same executable (e.g. git pull and git push).

Another thing you can do is to enumerate over a collection to include multiple template fragments in your output.  Take this example.

    {{each action in Actions}}
    {{action.DefaultAlias!}} - {{action.Description!}}
    !{{each}}

If your program has 3 actions defined then you'd get output like this.

    action1 - action 1 description here
    action2 - action 2 description here
    action3 - action 3 description here

When referring to a part of your data model you can also navigate objects using dot '.' notation.  Notice in the example above I was able to express {{ action.DefaultAlias !}}.  You could go even deeper.  For example {{ someObj.SomeProperty.DeeperProperty !}}.  More advanced expressions like function calling with parameters are not supported.

PS

I'm pretty happy with the templating solution.  In fact, hidden in PowerArgs is a general purpose template rendering engine that I've found useful in other projects for things like code generation.  You can actually bind any string template to any plain old .NET object (dynamic objects not supported).  Here's a basic sample:

```cs
var renderer = new DocumentRenderer();
var document = renderer.Render("Hi {{ Name !}}", new { Name = "Adam" });
// outputs 'Hi Adam'
```

###Ambient Args

Access your parsed command line arguments from anywhere in your application.

```cs
MyArgs parsed = Args.GetAmbientArgs<MyArgs>();
```
    
This will get the most recent insance of type MyArgs that was parsed on the current thread.  That way, you have access to things like global options without having to pass the result all throughout your code.


### Secure String Arguments

Support for secure strings such as passwords where you don't want your users' input to be visible on the command line.

Just add a property of type SecureStringArgument.

```cs
public class TestArgs
{
    public SecureStringArgument Password { get; set; }
}
```
    
Then when you parse the args you can access the value in one of two ways.  First there's the secure way.

```cs
TestArgs parsed = Args.Parse<TestArgs>();
SecureString secure = parsed.Password.SecureString; // This line causes the user to be prompted
```

Then there's the less secure way, but at least your users' input won't be visible on the command line.

```cs
TestArgs parsed = Args.Parse<TestArgs>();
string notSecure = parsed.Password.ConvertToNonsecureString(); // This line causes the user to be prompted
```

###Tab Completion

Get tab completion for your command line arguments.  Just add the TabCompletion attribute and when your users run the program from the command line with no arguments they will get an enhanced prompt (should turn blue) where they can have tab completion for command line argument names.

```cs
[TabCompletion]
public class TestArgs
{
    [ArgRequired]
    public string SomeParam { get; set; }
    public int AnotherParam { get; set; }
}
```

Sample usage:

    someapp -some  <-- after typing "-some" you can press tab and have it fill in the rest of "-someparam"

You can even add your own tab completion logic in one of two ways.  First there's the really easy way.  Derive from SimpleTabCompletionSource and provide a list of words you want to be completable.

```cs
public class MyCompletionSource : SimpleTabCompletionSource
{
    public MyCompletionSource() : base(MyCompletionSource.GetWords()) {}
    private static IEnumerable<string> GetWords()
    {
        return new string[] { "SomeLongWordThatYouWantToEnableCompletionFor", "SomeOtherWordToEnableCompletionFor" };
    }
}
``` 
 
 Then just tell the [TabCompletion] attribute where to find your class.

```cs 
[TabCompletion(typeof(MyCompletionSource))]
public class TestArgs
{
    [ArgRequired]
    public string SomeParam { get; set; }
    public int AnotherParam { get; set; }
}
```
 
 There's also the easy, but not really easy way if you want custom tab completion logic.  Let's say you wanted to load your auto completions from a text file.  You would implement ITabCompletionSource.
 
```cs
public class TextFileTabCompletionSource : ITabCompletionSource
{
    string[] words;
    public TextFileTabCompletionSource(string file)
    {
        words = File.ReadAllLines(file);
    }

    public bool TryComplete(bool shift, string soFar, out string completion)
    {
        var match = from w in words where w.StartsWith(soFar) select w;

        if (match.Count() == 1)
        {
            completion = match.Single();
            return true;
        }
        else
        {
            completion = null;
            return false;
        }
    }
}
```
    
If you expect your users to sometimes use the command line and sometimes run from a script then you can specify an indicator string.  If you do this then only users who specify the indicator as the only argument will get the prompt.

```cs
[TabCompletion("$")]
public class TestArgs
{
    [ArgRequired]
    public string SomeParam { get; set; }
    public int AnotherParam { get; set; }
}
```

###The PowerArgs.Cli (undocumented) namespace

The PowerArgs.Cli namespace contains framework components that make it easy to build very interactive command line applications.  This namespace is undocumented since these capabilities are still a work in progress.  When it gets closer to being ready I'll document the classes just like the rest of PowerArgs.

###Data Source Queries

Easily query a data source such as an Entity Framework Model (Code First or traditional) using Linq.

```cs
// An example Entity Framework Code First Data Model
public class DataSource : DbContext
{
    public DbSet<Customer> Customers{get;set;}
}

public class TestArgs
{
    public string OrderBy { get; set; }
    [ArgShortcut("o-")]
    public string OrderByDescending { get; set; }
    public string Where { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }

    [Query(typeof(DataSource))]
    [ArgIgnore]
    public List<Customer> Customers { get; set; }
}
```   
 
 That's it!  PowerArgs will make the query for you using the query arguments.  It's all done by naming convention.  
 
 Now just consume the data in your program.
    
```cs
// Sample command that queries the Customers table for newest 10 customers  
// <yourapp> -skip 0 -take 10 -where "item.DateCreated > DateTime.Now - TimeSpan.FromDays(1)" -orderby item.LastName

var parsed = Args.Parse<TestArgs>(args);
var customers = parsed.Customers;