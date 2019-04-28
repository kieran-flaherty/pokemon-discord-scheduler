using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;

namespace SendAPokemon
{
    class Program
    {
        public static AppConfig _config = new AppConfig();

        static void Main(string[] args)
        {
            LoadConfig();
            Task.Run(async () => await ScheduleJob());
            ReadInput();
        }

        /// <summary>
        /// Loads the config from the settings file into the _config object
        /// </summary>
        static void LoadConfig()
        {
            string currentDir = Directory.GetCurrentDirectory();

            try
            {
                Console.WriteLine("Getting Settings...");

                // Create config builder from json file
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(currentDir)
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile("appsettings_dev.json", true);

                // Bind config and bind to our config object
                configurationBuilder.Build().GetSection("Settings").Bind(_config);

                Console.WriteLine("Got Settings!");
            }
            catch (FileNotFoundException)
            {
                // Will throw if appsettings.json is missing
                Console.WriteLine($"Error: \"appsettings.json\" was not found in {currentDir}");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Uses Quartz to create a scheduler, job and trigger, then schedules the job
        /// </summary>
        static async Task ScheduleJob()
        {
            Console.WriteLine("Starting Scheduler...");
            // Create Scheduler instance and start it
            NameValueCollection schedulerFactoryProps = new NameValueCollection
            {
                 {"quartz.serializer.type","binary"}
            };
            StdSchedulerFactory schedulerFactory = new StdSchedulerFactory(schedulerFactoryProps);
            IScheduler scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            // Define Job
            IJobDetail sendPokemonJob = JobBuilder.Create<SendPokemonJob>()
                .WithIdentity("sendJob", "jobGroup")
                // Here we can pass data to job context
                .UsingJobData("pokemonBaseUrl",_config.PokemonUrl) 
                .UsingJobData("webHookUrl",_config.WebHook)
                .Build();

            // Build Trigger from app config
            TriggerBuilder triggerBuilder = TriggerBuilder.Create()
                .WithIdentity("sendTrigger", "jobGroup"); // (triggerName, groupName)

            // This switch case determines the lambda to pass to the trigger builders WithSimpleSchedule extension method
            Action<SimpleScheduleBuilder> scheduleBuilderLambda = null;
            switch (_config.IntervalType)
            {
                case (int)IntervalTypes.Seconds:
                    scheduleBuilderLambda = (x) => x.WithIntervalInSeconds(_config.Interval).RepeatForever();// Interval in seconds
                    break;
                case (int)IntervalTypes.Minutes:
                    scheduleBuilderLambda = (x) => x.WithIntervalInMinutes(_config.Interval).RepeatForever(); // Interval in minutes
                    break;
                case (int)IntervalTypes.Hours:
                    scheduleBuilderLambda = (x) => x.WithIntervalInHours(_config.Interval).RepeatForever(); // Interval in hours
                    break;
                case (int)IntervalTypes.Days:
                    scheduleBuilderLambda = (x) => x.WithIntervalInHours(_config.Interval) .RepeatForever(); // Interval in days
                    break;
                default:
                    Console.WriteLine("Interval Value Error, value must be between 0 and 3 inclusive");
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
            }

            triggerBuilder.WithSimpleSchedule(scheduleBuilderLambda); //Apply the schedule to the trigger builder

            ITrigger trigger = triggerBuilder.StartNow().Build(); // Build the trigger

            // Schedule Job
            await scheduler.ScheduleJob(sendPokemonJob, trigger);
        }

        static void ReadInput()
        {
            while(true)
            {
                string input = Console.ReadLine();
                switch (input)
                {
                    case "?":
                        Console.WriteLine("Help string goes here...");
                        break;
                    case "exit":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Press ? for help");
                        break;
                }
            }
        }
    }
}
