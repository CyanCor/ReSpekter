// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cyan Cor" file="Versioning.cs">
//   Copyright (c) 2013 Cyan Cor. All rights reserved.
// </copyright>
// <summary>
//   Defines the Versioning type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#pragma DNA:Reference("mscorlib.dll")
#pragma DNA:Reference("System.dll")
#pragma DNA:Reference("System.Core.dll")
#pragma DNA:Reference("System.Xml.dll")
#pragma DNA:Reference("System.Xml.Linq.dll")
#pragma DNA:End()

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using CyanCor.DNA;

namespace CyanCor.SourceControl
{
    public class Git
    {
        public Git()
        {
            GetLastCommitInformation();
            GetTagInformation();
        }

        public string Branch
        {
            get
            {
                string output;
                string error;
                Tools.CommandLineExecute("git", "branch", out output, out error);

                if (string.IsNullOrEmpty(output))
                {
                    return string.Empty;
                }
                else
                {
                    string[] branches = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string result = branches.First((s) => s.Contains('*'));
                    if (string.IsNullOrEmpty(result))
                    {
                        return string.Empty;
                    }
                    return result.Trim(new[] { '\t', ' ', '*', '\n', '\r' });
                }
            }
        }
		
        public string Hash { get; private set; }
        public string ShortHash { get; private set; }
        public string Author { get; private set; }
        public string AuthorEmail { get; private set; }
        public DateTime AuthorDate { get; private set; }
        public string Committer { get; private set; }
        public string CommitterEmail { get; private set; }
        public DateTime CommitterDate { get; private set; }
        public string Subject { get; private set; }
		public string Version { get; private set; }
		public int CommitsAfterTag { get; private set; }
        public string ReleaseType { get; private set; }
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Patch { get; private set; }


        internal string CommitterDateString
        {
            set
            {
                DateTime dt;
                DateTime.TryParse(value, out dt);
                CommitterDate = dt;
            }
        }

        internal string AuthorDateString
        {
            set
            {
                DateTime dt;
                DateTime.TryParse(value, out dt);
                AuthorDate = dt;
            }
        }
		
		private void GetLastCommitInformation()
		{
            string output;
            string error;
			
			string format = "Hash:%H%n" + 
							"ShortHash:%h%n" + 
							"Author:%an%n" +
							"AuthorEmail:%ae%n" +
							"AuthorDateString:%ai%n" +
							"Committer:%cn%n" +
							"CommitterEmail:%ce%n" +
							"CommitterDateString:%ci%n" +
							"Subject:%s%n";
			
            int returnCode = Tools.CommandLineExecute("git", "log -n 1 --format=" + format, out output, out error);

		    if (returnCode != 0)
		    {
                Console.WriteLine("Failed to get commit information. Possible causes: No commit has been made yet; Git is not installed or not in PATH; This is not a git repository.");
		        Hash = "0000000000000000000000000000000000000000";
		        ShortHash = "0000000";
		        return;
		    }

			var properties = this.GetType().GetProperties(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty);
			
            string[] lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				string propertyName = line.Substring(0, line.IndexOf(':'));
				string value = line.Substring(propertyName.Length + 1).Trim();
				
				var property = properties.FirstOrDefault(prop => prop.Name == propertyName);
				if (property == null)
				{
					continue;
				}
				
				property.SetValue(this, value, null);
			}
		}
		
		private void GetTagInformation()
		{
            string output;
            string error;
			
            int returnCode = Tools.CommandLineExecute("git", "describe --tags --long", out output, out error);

		    if (returnCode != 0 || string.IsNullOrWhiteSpace(output))
		    {
                Console.WriteLine("Failed to get tag information. Possible causes: No tag has been set yet; Git is not installed or not in PATH; This is not a git repository.");
		        output = "dev-0.0.0-0-g" + ShortHash;
		    }

            Regex reg = new Regex("^([a-z]+)-([0-9]+)\\.([0-9]+)\\.([0-9]+)(-([0-9]+)-g([0-9a-fA-F]+))?$");

            var match = reg.Match(output.Trim());
            int major = 0;
            int minor = 0;
            int patch = 0;
            int commits = 0;
		    if (match.Success)
		    {
		        Version = match.Groups[0].Value;

                ReleaseType = match.Groups[1].Value;
		        int.TryParse(match.Groups[2].Value, out major);
                int.TryParse(match.Groups[3].Value, out minor);
                int.TryParse(match.Groups[4].Value, out patch);
                int.TryParse(match.Groups[6].Value, out commits);

		        Major = major;
		        Minor = minor;
		        Patch = patch;
		        CommitsAfterTag = commits;
		    }
		    else
		    {
		        Version = output;
                Console.WriteLine("Error: Invalid Tag!");
                ReleaseType = "unknown";
		    }

            Console.WriteLine("Release: " + ReleaseType);
            Console.WriteLine("Major: " + Major);
            Console.WriteLine("Minor: " + Minor);
            Console.WriteLine("Patch: " + Patch);
            Console.WriteLine("CommitsAfterTag:" + CommitsAfterTag);
            Console.WriteLine(match.Groups[7]);
            Console.WriteLine(Hash);
		}
    }
}
