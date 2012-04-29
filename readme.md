###Binary
Just want to add a reference instead of dealing with the source? PowerArgs is available at the [Official NuGet Gallery](http://nuget.org/packages/PowerArgs).

###Basic Example
    
    // A class that describes the command line arguments for this program
    public class MyArgs
    {
        // This argument is required and if not specified the user will 
        // be prompted.
        [ArgRequired(PromptIfMissing=true)]
        public string StringArg { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<MyArgs>(args);
                Console.WriteLine("You entered '{0}'", parsed.StringArg);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GetUsage<MyArgs>());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
    
###Metadata Attributes 
These can be specified on argument properties.
 
    [ArgPosition(0)]                                    // This argument can be specified by position (no need for -propName)
    [ArgShortcut("n")]                                  // Let's the user specify -n
    [ArgDescription("Description of the argument")]
    [ArgExample("example text", "Example description")]
    [DefaultValue("SomeDefault")]                       // Specify the default value
    [ArgIgnore]                                         // Don't populate this property as an arg
    [StickyArg]                                         // Use the last used value if not specified
    [Query(typeof(MyDataSource))]                       // Easily query a data source
    
###Validator Attributes
These can be specified on argument properties.  You can create custom validators by implementing classes that derive from ArgValidator.

    [ArgRequired]
    [ArgExistingFile]
    [ArgExistingDirectory]
    [ArgRange]
    
###Latest Features

Easily query a data source such as an Entity Framework Model (Code First or traditional) using Linq.

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
 
 That's it!  PowerArgs will make the query for you using the query arguments.  It's all done by naming convention.  
 
 Now just consume the data in your program.
    
    // Sample command that queries the Customers table for newest 10 customers  
    // <yourapp> -skip 0 -take 10 -where "item.DateCreated > DateTime.Now - TimeSpan.FromDays(1)" -orderby item.LastName
    
    var parsed = Args.Parse<TestArgs>(args);
    var customers = parsed.Customers;
    
###Advanced Example
This example shows various metadata and validator attributes.  It also uses the Action Framework that lets you separate your program into distinct actions that take different parameters.  In this case there are 2 actions called Encode and Clip.  It also shows how enums are supported.

    [ArgExample("superencoder encode fromFile toFile -encoder Wmv", "Encode the file at 'fromFile' to an AVI at 'toFile'")]
    public class VideoEncoderArgs
    {
        // To use the action framework, the action arg must be called "Action"
        // and must be a required parameter at position 0.
        //
        // If you're using the Action Framework then don't use Args.Parse<VideoEncoderArgs>(args).
        // Instead use Args.InvokeAction<VideoEncoderArgs>(args).  That will not only parse the
        // arguments, but it will also map the user's specified action to an action property,
        // populate that property, and finally invoke the action method.
        // There is an example of this below.
        
    
        [ArgRequired]
        [ArgPosition(0)]
        [ArgDescription("Either encode or clip")]
        public string Action { get; set; }

        // See the two properties below.  They are action properties.  If 
        // your class has the "Action" property configured correctly then all
        // remaining properties that end with "Args" will be considered actions
        // that the user can enter as their first command line value.
        // 
        // In this case the end user could enter "superencoder encode" or
        // "superencode clip".  Based on the action parameter the rest of the
        // arguments will be used to populate the matching action property.

        [ArgDescription("Encode a new video file")]
        public EncodeArgs EncodeArgs { get; set; }

        [ArgDescription("Save a portion of a video to a new video file")]
        public ClipArgs ClipArgs { get; set; }

        public static void Encode(EncodeArgs args)
        {
            // TODO - Your action code
        }

        public static void Clip(ClipArgs args) 
        {
            // TODO - Your action code
        }
    }

    public enum Encoder
    {
        Mpeg,
        Avi,
        Wmv
    }

    public class EncodeArgs
    {
        [ArgRequired]
        [ArgExistingFile]
        [ArgPosition(1)]
        [ArgDescription("The source video file")]
        public string Source { get; set; }

        [ArgPosition(2)]
        [ArgDescription("Output file.  If not specfied, defaults to current directory")]
        public string Output { get; set; }

        [DefaultValue(Encoder.Avi)]
        [ArgDescription("The type of encoder to use")]
        public Encoder Encoder { get; set; }
    }

    public class ClipArgs : EncodeArgs
    {
        [ArgRange(0, double.MaxValue)]
        [ArgDescription("The starting point of the video, in seconds")]
        public double From { get; set; }
            
        [ArgRange(0, double.MaxValue)]
        [ArgDescription("The ending point of the video, in seconds")]
        public double To { get; set; }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Args.InvokeAction<VideoEncoderArgs>(args);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GetUsage<VideoEncoderArgs>());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
    
###Custom Revivers
Revivers are used to convert command line strings into their proper .NET types.  By default, many of the simple types such as int, DateTime, Guid, string, char,  and bool are supported.

If you need to support a different type or want to support custom syntax to populate a complex object then you can create a custom reviver.

This example converts strings in the format "x,y" into a Point object that has properties "X" and "Y".

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

###Auto-Generated Usage
You can get an auto generated usage string by passing your argument type to the GetUsage<T> function like this:

    string usage = ArgUsage.GetUsage<VideoEncoderArgs>();

Sample Output:

    Usage: superencoder <action> options

    EXAMPLE: superencoder encode fromFile toFile -encoder Wmv
    Encode the file at 'fromFile' to an AVI at 'toFile'

    Global options:

       OPTION         TYPE     ORDER   DESCRIPTION                       
       -whatif (-w)   Switch           Simulate the encoding operation   

    Actions:

    Encode - Encode a new video file

       OPTION          TYPE      ORDER   DESCRIPTION                                                    
       -source (-s)    String*   1       The source video file                                          
       -output (-o)    String    2       Output file.  If not specfied, defaults to current directory   
       -encoder (-e)   Encoder           The type of encoder to use                                     


    Clip - Save a portion of a video to a new video file

       OPTION          TYPE      ORDER   DESCRIPTION                                                    
       -source (-s)    String*   1       The source video file                                          
       -output (-o)    String    2       Output file.  If not specfied, defaults to current directory   
       -from (-f)      Double            The starting point of the video, in seconds                    
       -to (-t)        Double            The ending point of the video, in seconds                      
       -encoder (-e)   Encoder           The type of encoder to use  

This was the code that resulted in the generated usage string.

    [ArgExample("superencoder encode fromFile toFile -encoder Wmv", "Encode the file at 'fromFile' to an AVI at 'toFile'")]
    public class VideoEncoderArgs
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgDescription("Either encode or clip")]
        public string Action { get; set; }

        [ArgDescription("Encode a new video file")]
        public EncodeArgs EncodeArgs { get; set; }
        [ArgDescription("Save a portion of a video to a new video file")]
        public ClipArgs ClipArgs { get; set; }

        [ArgDescription("Simulate the encoding operation")]
        public bool WhatIf { get; set; }

        public static void Encode(EncodeArgs args)
        {
            // TODO - Implement Encode Action
        }

        public static void Clip(ClipArgs args) 
        {
            // TODO - Implement Clip action
        }
    }

    public enum Encoder
    {
        Mpeg,
        Avi,
        Wmv
    }

    public class EncodeArgs
    {
        [ArgRequired]
        [ArgExistingFile]
        [ArgPosition(1)]
        [ArgDescription("The source video file")]
        public string Source { get; set; }

        [ArgPosition(2)]
        [ArgDescription("Output file.  If not specfied, defaults to current directory")]
        public string Output { get; set; }

        [DefaultValue(Encoder.Avi)]
        [ArgDescription("The type of encoder to use")]
        public Encoder Encoder { get; set; }
    }

    public class ClipArgs : EncodeArgs
    {
        [ArgRange(0, double.MaxValue)]
        [ArgDescription("The starting point of the video, in seconds")]
        public double From { get; set; }
            
        [ArgRange(0, double.MaxValue)]
        [ArgDescription("The ending point of the video, in seconds")]
        public double To { get; set; }
    }                                   
