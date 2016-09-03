using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Preview;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace ArgsTests
{
    [TestClass]
    public class PipelineTests
    {
        public class Customer
        {
            public string Name { get; set; }
            public string AccountNumber { get; set; }
        }


        [ArgPipeline]
        public class PipeableArgs
        {
            [ArgIgnore]
            public Guid Id { get; private set; }
            public static event Action<Customer> CustomerDeleted;
            public static event Action<Customer, string> CustomerEmailed;

            public static List<Customer> Customers;

            public PipeableArgs()
            {
                Id = Guid.NewGuid();
                if(Customers == null)
                {
                    Customers = new List<Customer>();
                    for(int i = 0; i < 10; i++)
                    {
                        Customers.Add(new Customer { Name = "Customer-"+i, AccountNumber = ""+i });
                    }
                }
            }

            [ArgActionMethod]
            public void GetCustomer(string accountNumber)
            {
                var match = (from c in Customers where c.AccountNumber == accountNumber select c).SingleOrDefault();
                if (match != null)
                {
                    ArgPipeline.Push(match);
                }
            }


            [ArgActionMethod]
            public void GetCustomers()
            {
                foreach(var customer in Customers)
                {
                    ArgPipeline.Push(customer);
                }
            }

            [ArgActionMethod]
            public void GetNumbers([ArgDefaultValue(10)]int amount)
            {
                for(int i = 0; i < amount; i++)
                {
                    ArgPipeline.Push(i);
                }
            }

            [ArgActionMethod]
            public void DeleteCustomer(string accountNumber)
            {
                var customer = (from c in Customers where c.AccountNumber == accountNumber select c).SingleOrDefault();
                if(customer == null)
                {
                    ConsoleString.WriteLine("There is no account with account number: " + accountNumber, ConsoleColor.Red);
                }
                else
                {
                    ArgPipeline.Push(customer);
                    if(CustomerDeleted != null)
                    {
                        CustomerDeleted(customer);
                    }
                }
            }

            [ArgActionMethod]
            public void EmailCustomer(string accountNumber, string message)
            {
                var customer = (from c in Customers where c.AccountNumber == accountNumber select c).SingleOrDefault();
                if (customer == null)
                {
                    ConsoleString.WriteLine("There is no account with account number: " + accountNumber, ConsoleColor.Red);
                }
                else
                {
                    ConsoleString.WriteLine("Emailed customer '" + customer.Name + "' with message '" + message + "'");
                    ArgPipeline.Push(customer);
                    if (CustomerEmailed != null)
                    {
                        CustomerEmailed(customer, message);
                    }
                }
            }
        }

        
        [ArgPipeline, UnitTestEXEAttribute]
        public class PipeableArgsWithDirectPipelining
        {
            [ArgIgnore]
            public Guid Id { get; private set; }
            public static event Action<Customer> CustomerDeleted;

            public static List<Customer> Customers;

            public PipeableArgsWithDirectPipelining()
            {
                Id = Guid.NewGuid();
                if (Customers == null)
                {
                    Customers = new List<Customer>();
                    for (int i = 0; i < 10; i++)
                    {
                        Customers.Add(new Customer { Name = "Customer-" + i, AccountNumber = "" + i });
                    }
                }
            }

            [ArgActionMethod]
            public void GetCustomers()
            {
                foreach (var customer in Customers)
                {
                    ArgPipeline.Push(customer);
                }
            }

            [ArgActionMethod]
            public void DeleteCustomer([ArgPipelineTarget, ArgRequired(IfNot = "customerId")]                               Customer pipedCustomer, 
                                       [ArgCantBeCombinedWith("pipedCustomer"), ArgRequired(IfNot = "pipedCustomer")]       string customerId)
            {
                ArgPipeline.Push(pipedCustomer);
                if (CustomerDeleted != null)
                {
                    CustomerDeleted(pipedCustomer);
                }
            }
        }

        [TestMethod]
        public void TestPipelineOnlyArgsDontShowInUsage()
        {
            var usage = ArgUsage.GenerateUsageFromTemplate<PipeableArgsWithDirectPipelining>().ToString().ToLowerInvariant();
            var usage2 = ArgUsage.GenerateUsageFromTemplate<PipeableArgsWithDirectPipelining>(PowerArgs.Resources.DefaultBrowserUsageTemplate).ToString().ToLowerInvariant();
            Assert.IsFalse(usage.Contains("pipedCustomer".ToLowerInvariant()));
            Assert.IsFalse(usage2.Contains("pipedCustomer".ToLowerInvariant()));
        }

        [TestMethod]
        public void TestDirectPipelineMapping()
        {
            var action = Args.ParseAction<PipeableArgsWithDirectPipelining>("getcustomers", "=>", "deletecustomer");

            var expectedDeletions = PipeableArgsWithDirectPipelining.Customers.Select(c => c.Name).ToList();

            Action<Customer> deletedHandler = (customer) =>
            {
                Assert.AreEqual(expectedDeletions.First(), customer.Name);
                expectedDeletions.RemoveAt(0);
            };

            PipeableArgsWithDirectPipelining.CustomerDeleted += deletedHandler;
            Assert.AreEqual(PipeableArgsWithDirectPipelining.Customers.Count, expectedDeletions.Count);


            try
            {
                action.Invoke();
            }
            finally
            {
                PipeableArgsWithDirectPipelining.CustomerDeleted -= deletedHandler;
            }

            Assert.AreEqual(0, expectedDeletions.Count);

            Console.WriteLine("expected deletions count: " + expectedDeletions);
        }

        [TestMethod]
        public void TestDirectPipelineMappingWithConditionalRequired()
        {
            try
            {
                Args.ParseAction<PipeableArgsWithDirectPipelining>("deletecustomer");
                Assert.Fail("An exception should have been thrown");
            }
            catch(MissingArgException)
            {

            }

            try
            {
                Args.InvokeAction<PipeableArgsWithDirectPipelining>("getcustomers", "=>", "deletecustomer", "-customerId", "foo");
                Assert.Fail("An exception should have been thrown");
            }
            catch (AggregateException ex)
            {
                Assert.IsTrue(ex.InnerExceptions.Count > 1);
                foreach(var exception in ex.InnerExceptions)
                {
                    Assert.IsInstanceOfType(exception, typeof(UnexpectedArgException));
                }
            }
            catch(UnexpectedArgException)
            {
                // the case when there's only 1 exception
            }
        }


        [TestMethod]
        public void TestBasicPipeline()
        {
            var action = Args.ParseAction<PipeableArgs>("getcustomers", "=>", "deletecustomer", "=>", "emailcustomer", "-message", "You have been deleted!");

            var expectedDeletions = PipeableArgs.Customers.Select(c => c.Name).ToList();
            var expectedEmails = PipeableArgs.Customers.Select(c => c.Name).ToList();

            Action<Customer> deletedHandler = (customer) =>
            {
                Assert.AreEqual(expectedDeletions.First(), customer.Name);
                expectedDeletions.RemoveAt(0);
            };

            Action<Customer, string> emailedHandler = (customer, message) =>
            {
                Assert.AreEqual("You have been deleted!", message);
                Assert.AreEqual(expectedEmails.First(), customer.Name);
                expectedEmails.RemoveAt(0);
            };

            PipeableArgs.CustomerDeleted += deletedHandler;
            PipeableArgs.CustomerEmailed += emailedHandler;

            Assert.AreEqual(PipeableArgs.Customers.Count, expectedDeletions.Count);
            Assert.AreEqual(PipeableArgs.Customers.Count, expectedEmails.Count);

            try
            {
                action.Invoke();
            }
            finally
            {
                PipeableArgs.CustomerDeleted -= deletedHandler;
                PipeableArgs.CustomerEmailed -= emailedHandler;
            }

            Assert.AreEqual(0, expectedDeletions.Count);
            Assert.AreEqual(0, expectedEmails.Count);

            Console.WriteLine("expected deletions count: "+expectedDeletions);
            Console.WriteLine("expected emails count: " + expectedEmails);
        }

        [TestMethod]
        public void TestPipelineManualOverride()
        {
            // not a great example, but manually specifying '2' in the delete command should override the '1' that comes out of getcustomer.
            var action = Args.ParseAction<PipeableArgs>("getcustomer", "-accountNumber", "1", "=>", "deletecustomer", "-accountNumber", "2");

            bool deleteHappened = false;
            Action<Customer> deletedHandler = (customer) =>
            {
                Assert.AreEqual("2", customer.AccountNumber);
                deleteHappened = true;
            };

            PipeableArgs.CustomerDeleted += deletedHandler;

            try
            {
                action.Invoke();
            }
            finally
            {
                PipeableArgs.CustomerDeleted -= deletedHandler;
            }

            Assert.IsTrue(deleteHappened);
        }
        
        [TestMethod]
        public void TestPipelineFilter()
        {
            List<object> pipeOutput = new List<object>();
            Action<object> pipeHandler = (o) =>
            {
                pipeOutput.Add(o);
            };

            ArgPipeline.ObjectExitedPipeline += pipeHandler;

            try
            {
                Args.InvokeAction<PipeableArgs>("getcustomers", "=>", "$filter", "AccountNumber", ">", "4");
                Assert.AreEqual(5, pipeOutput.Count);
                foreach(Customer c in pipeOutput)
                {
                    Assert.IsTrue(c.AccountNumber.CompareTo("4") > 0);
                }
            }
            finally
            {
                ArgPipeline.ObjectExitedPipeline -= pipeHandler;
            }
        }

        [TestMethod]
        public void TestPipelineFilterOperators()
        {
            Assert.IsTrue(FilterLogic.FilterAcceptsObject("a", FilterOperators.Equals, "a"));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject("a", FilterOperators.Equals, "b"));

            Assert.IsTrue(FilterLogic.FilterAcceptsObject("a", FilterOperators.NotEquals, "b"));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject("a", FilterOperators.NotEquals, "a"));

            Assert.IsTrue(FilterLogic.FilterAcceptsObject(2, FilterOperators.GreaterThan, 1));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject(1, FilterOperators.GreaterThan, 1));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject(0, FilterOperators.GreaterThan, 1));

            Assert.IsTrue(FilterLogic.FilterAcceptsObject(1, FilterOperators.LessThan, 2));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject(2, FilterOperators.LessThan, 2));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject(2, FilterOperators.LessThan, 1));

            Assert.IsTrue(FilterLogic.FilterAcceptsObject(1, FilterOperators.GreaterThanOrEqualTo, 1));
            Assert.IsTrue(FilterLogic.FilterAcceptsObject(2, FilterOperators.GreaterThanOrEqualTo, 1));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject(1, FilterOperators.GreaterThanOrEqualTo, 2));

            Assert.IsTrue(FilterLogic.FilterAcceptsObject(1, FilterOperators.LessThanOrEqualTo, 1));
            Assert.IsTrue(FilterLogic.FilterAcceptsObject(0, FilterOperators.LessThanOrEqualTo, 1));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject(1, FilterOperators.LessThanOrEqualTo, 0));

            Assert.IsTrue(FilterLogic.FilterAcceptsObject("Adam", FilterOperators.Contains, "A"));
            Assert.IsFalse(FilterLogic.FilterAcceptsObject("Adam", FilterOperators.Contains, "B"));
        }

        [TestMethod]
        public void TestPipelineTableComplex()
        {
            bool tableWrittenFired = false;
            Action<string, List<object>, ConsoleString> tableWrittenHandler = (template, objects, output) =>
            {
                Assert.IsTrue(template.Contains("Name AccountNumber"));
                Assert.IsTrue(output.ToString().Contains("Name"));
                Assert.IsTrue(output.ToString().Contains("AccountNumber"));
                Assert.IsTrue(output.ToString().Contains("Customer-0"));
                Assert.IsTrue(output.ToString().Contains("Customer-1"));
                Assert.IsTrue(output.ToString().Contains("Customer-2"));
                Assert.IsTrue(output.ToString().Contains("Customer-3"));
                Assert.IsTrue(output.ToString().Contains("Customer-4"));
                Assert.IsTrue(output.ToString().Contains("Customer-5"));
                Assert.IsTrue(output.ToString().Contains("Customer-6"));
                Assert.IsTrue(output.ToString().Contains("Customer-7"));
                Assert.IsTrue(output.ToString().Contains("Customer-8"));
                Assert.IsTrue(output.ToString().Contains("Customer-9"));
                tableWrittenFired = true;
            };

            Table.TableWritten += tableWrittenHandler;

            try
            {
                List<object> pipeOutput = new List<object>();
                Args.InvokeAction<PipeableArgs>("getcustomers", "=>", "$table");
            }
            finally
            {
                Table.TableWritten -= tableWrittenHandler;
            }

            Assert.IsTrue(tableWrittenFired);
        }

        [TestMethod]
        public void TestPipelineTablePrimitive()
        {
            bool tableWrittenFired = false;
            Action<string, List<object>, ConsoleString> tableWrittenHandler = (template, objects, output) =>
            {
                Assert.IsTrue(template.Contains("item"));
                Assert.IsTrue(output.ToString().Contains("0"));
                Assert.IsTrue(output.ToString().Contains("1"));
                Assert.IsTrue(output.ToString().Contains("2"));
                Assert.IsTrue(output.ToString().Contains("3"));
                Assert.IsTrue(output.ToString().Contains("4"));
                Assert.IsTrue(output.ToString().Contains("5"));
                Assert.IsTrue(output.ToString().Contains("6"));
                Assert.IsTrue(output.ToString().Contains("7"));
                Assert.IsTrue(output.ToString().Contains("8"));
                Assert.IsTrue(output.ToString().Contains("9"));
  
                tableWrittenFired = true;
            };

            Table.TableWritten += tableWrittenHandler;

            try
            {
                List<object> pipeOutput = new List<object>();
                Args.InvokeAction<PipeableArgs>("getnumbers", "10", "=>", "$table");
            }
            finally
            {
                Table.TableWritten -= tableWrittenHandler;
            }

            Assert.IsTrue(tableWrittenFired);
        }

        internal class UnitTestCollapse : InProcessPipelineStage
        {
            List<object> objects = new List<object>();
            public UnitTestCollapse(string[] commandLine)
                : base(commandLine)
            {
                if (commandLine.Length > 0) throw new ArgException("UnitTestCollapse takes no command line input");
            }

            protected override void OnObjectReceived(object o)
            {
                lock(objects)
                {
                    objects.Add(o);
                }
            }

            protected override void BeforeSetDrainedToTrue()
            {
                ArgPipeline.Push(objects);
            }
        }

        [TestMethod]
        public void TestPipelineInjectActionStage()
        {
            ArgPipelineActionStage.RegisterActionStage("UnitTestCollapse", typeof(UnitTestCollapse));

            bool outputFired = false;

            Action<object> pipeHandler = (o) =>
            {
                if(outputFired)
                {
                    outputFired = false;
                    Assert.Fail("Output fired more than once");
                }
                int count = 0;
                foreach(var item in o as IEnumerable)
                {
                    count++;
                }

                Assert.AreEqual(10, count);
                outputFired = true;
            };

            ArgPipeline.ObjectExitedPipeline += pipeHandler;

            try
            {
                Args.InvokeAction<PipeableArgs>("getcustomers", "=>", "$UnitTestCollapse");
                Assert.IsTrue(outputFired);
            }
            finally
            {
                ArgPipeline.ObjectExitedPipeline -= pipeHandler;
            }
        }
    }
}
