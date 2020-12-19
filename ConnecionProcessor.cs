using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SocketTcpServer {
    public struct RequestData
    {
        public string Command { get; set; }
        public string Proc { get; set; }
        public string Data { get; set; }
        public RequestData(string command, string proc, string data)
        {
            Command = command;
            Proc = proc;
            Data = data;
        }
    }
    public struct ResponseData
    {
        public string Error { get; set; }
        public string Result { get; set; }
        public string Axe1Name { get; set; }
        public string Axe2Name { get; set; }
        public NamedSeries[] Values { get; set; }
        public ResponseData(string error, string result, string axe1Name, string axe2Name, NamedSeries[] values)
        {
            Error = error; Result = result;
            Values = values;
            Axe1Name = axe1Name; Axe2Name = axe2Name;
        }
    }
    public struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Point(double x, double y)
        {
            this.X = x; this.Y = y;
        }
    }

    public struct NamedSeries
    {
        public Collection<Point> Series { get; set; }
        public string Name { get; set; }
        public NamedSeries(string name, Collection<Point> series)
        {
            Name = name; Series = series;
        }
    }

    class RequestProcessor {

        public RequestProcessor() {

        }

        public string ProcessRequest(string request) {
            RequestData requestData = JsonSerializer.Deserialize<RequestData>(request);
            string result = "";
            ResponseData subres = new ResponseData();
            switch (requestData.Command) {
            case "search":
                subres = SearchForFunctions();
                break;
            case "info":
                // subres = PythonRun(requestData.Proc, "info");
                // setFile(requestData.Proc);
                subres = getInfo(requestData.Proc);
                break;
            case "eval":
                // subres = PythonRun(requestData.Proc, "eval " + requestData.Data);
                subres = evalProc(requestData.Proc, requestData.Data);
                break;
            }
            result = JsonSerializer.Serialize<ResponseData>(subres);
            return result;
        }

        public static string pathToProc = AppDomain.CurrentDomain.BaseDirectory + "Functions/";
        public static string pathPython = "I:\\Python\\python.exe";

        public ResponseData SearchForFunctions() {
            string[] files = Directory.GetFiles(pathToProc, "*");
            files = new List<string>(files).Select(f => f.Substring(f.LastIndexOf('/') + 1)).ToArray();
            return new ResponseData("", JsonSerializer.Serialize(files), "", "", null);
        }

        /// <summary>
        /// Evaluate expression writed on Python and used Python function
        /// </summary>
        /// <param name="expression">expression to evaluation</param>
        /// <returns>string represents double value</returns>
        public static ResponseData PythonRun(string filename, string mode, string parameters) {
            return run_cmd(pathToProc + filename, mode+" \""+parameters+"\"");
        }

        /// <summary>
        /// Run command using cmd
        /// </summary>
        /// <param name="cmd">Command to execute (for example .exe file)</param>
        /// <param name="args">Arguments with which command will execute</param>
        /// <returns>ResponseData from command output</returns>
        public static ResponseData run_cmd(string cmd, string args) {
            ProcessStartInfo start = new ProcessStartInfo();
            // start.FileName = "I:\\Python\\python.exe";
            start.FileName = pathPython;
            start.Arguments = string.Format("{0} {1}", cmd, args);
            Console.WriteLine(start.Arguments);
            start.UseShellExecute = false; // Do not use OS shell
            start.CreateNoWindow = true; // We don't need new window
            start.RedirectStandardOutput = true; // Any output, generated by application will be redirected back
            start.RedirectStandardError = true; // Any error in standard output will be redirected back (for example exceptions)
            try{
            using(Process process = Process.Start(start)) {
                using(StreamReader reader = process.StandardOutput) {
                    string stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from our Python script
                    string result = reader.ReadToEnd(); // Here is the result of StdOut(for example: print "test")
                    ResponseData data;
                    try{
                        data = JsonSerializer.Deserialize<ResponseData>(result);
                    } catch (Exception e){
                        data = new ResponseData();
                    }
                    if (Regex.IsMatch(stderr, @".*No such file or directory.*")){
                        data.Error+="Function unavailable\nPlease request for procedure list";
                    } else {
                        data.Error+=stderr;
                    }
                    return data;
                }
            }
            } catch (Exception e){
                return new ResponseData("Error "+e, "", "", "", null);
            }
        }

        public static ResponseData getInfo(string package) {
            return PythonRun(package, "info", null);
        }

        public static ResponseData evalProc(string package, string parameters){
            return PythonRun(package, "eval", parameters);
        }

    }

}