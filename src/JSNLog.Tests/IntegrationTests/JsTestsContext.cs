﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xunit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace JSNLog.Tests.IntegrationTests
{
    public class JsTestsContext : IDisposable
    {
        public IWebDriver Driver
        {
            get; private set;
        }

        // The port 5000 is always used by kestrel
        private const string _baseUrl = "http://localhost:5000";
        private readonly Process _serverProcess;

        public JsTestsContext()
        {
            string jsnlogTestsProjectDirectory = Directory.GetCurrentDirectory();
            string jsnlogTestSiteProjectDirectory = Path.Combine(jsnlogTestsProjectDirectory, "..", "JSNLog.TestSite");

            _serverProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "dnx.exe",
                Arguments = "web",
                WorkingDirectory = jsnlogTestSiteProjectDirectory
            });

            Thread.Sleep(3000);

            if (_serverProcess.HasExited)
            {
                throw new Exception(string.Format("Kestrel server could not be started - exit code: {0}. Before running these tests, " +
                    "make sure Kestrel is not already running.", 
                    _serverProcess.ExitCode));
            }



//##################
            //string processErrors = _serverProcess.StandardError.ReadToEnd();
            //if (!string.IsNullOrEmpty(processErrors))
            //{
            //}

            // ----------------------

            // To use ChromeDriver, you must have chromedriver.exe. Download from
            // https://sites.google.com/a/chromium.org/chromedriver/downloads

            //TODO: fix hard coding of path to dependencies folder
            // The following code works fine in .Net 40, but in DNX451 executingAssemblyLocation is set to "".
            // So hard code the path for now.
            //var executingAssembly = Assembly.GetExecutingAssembly();
            //var executingAssemblyLocation = executingAssembly.Location;
            //string assemblyFolder = Path.GetDirectoryName(executingAssemblyLocation);
            //string dependenciesFolder = Path.Combine(assemblyFolder, "Dependencies");

            string dependenciesFolder = Path.Combine(jsnlogTestsProjectDirectory, "IntegrationTests", "Dependencies");
            Driver = new ChromeDriver(dependenciesFolder);
        }

        public void Dispose()
        {
            // Close the browser if there is no error. Otherwise leave open.
            if (!ErrorOnPage())
            {
                _serverProcess.Kill();
                Driver.Quit();
            }
        }

        public void OpenPage(string relativeUrl)
        {
            string absoluteUrl = _baseUrl + relativeUrl;
            Driver.Navigate().GoToUrl(absoluteUrl);
        }

        /// <summary>
        /// Returns true if there is an error element on the page, or if the "test running" message is still on the page
        /// (meaning the test js crashed).
        /// </summary>
        /// <returns></returns>
        public bool ErrorOnPage()
        {
            // Check for C# exception
            bool unhandledExceptionOccurred = Driver.PageSource.Contains("An unhandled exception occurred");
            bool noConnection = Driver.PageSource.Contains("ERR_CONNECTION_REFUSED");

            if (unhandledExceptionOccurred || noConnection)
            {
                return true;
            }
            
            try
            {
                // Throws NoSuchElementException if error-occurred not found
                Driver.FindElement(By.Id("loaded"));
            }
            catch (NoSuchElementException)
            {
                // page never even loaded
                return true;
            }

            try
            {
                // Throws NoSuchElementException if error-occurred not found
                Driver.FindElement(By.ClassName("error-occurred"));
            }
            catch (NoSuchElementException)
            {
                try
                {
                    // Throws NoSuchElementException if running not found
                    Driver.FindElement(By.Id("running"));
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

