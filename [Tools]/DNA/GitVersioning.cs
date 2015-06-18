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
        
        public string Hash { get; set; }
        public string ShortHash { get; set; }
        public string ReleaseType { get; set; }

        public string Prefix { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public int CommitsAfterTag { get; set; }
        public string Postfix { get; set; }
        public string Version { get; set; }
        public string PlainVersion { get; set; }
        public string ShortVersion { get; set; }

        public Git()
        {
            GetLastCommitInformation();
            int branchMajor, branchMinor, branchPatch;
            var branch = ReadBranch(out branchMajor, out branchMinor, out branchPatch);
            int tagMajor, tagMinor, tagPatch, tagCommits;
            ReadTagVersion(out tagMajor, out tagMinor, out tagPatch, out tagCommits);

            if (branch.Equals("master"))
            {
                ReleaseType = "production";
                Major = tagMajor;
                Minor = tagMinor;
                Patch = tagPatch;
                CommitsAfterTag = tagCommits;
                Postfix = string.Empty;
            }
            else if (branch.StartsWith("release-"))
            {
                ReleaseType = "beta";
                Major = branchMajor;
                Minor = branchMinor;
                Patch = branchPatch;
                CommitsAfterTag = tagCommits;
                Postfix = "." + CommitsAfterTag + "-" + ShortHash;
            }
            else if (branch.Equals("develop"))
            {
                ReleaseType = "dev";
                Major = tagMajor;
                Minor = tagMinor;
                Patch = tagPatch;
                CommitsAfterTag = tagCommits;
                Postfix = "." + tagCommits + "-" + ShortHash;
            }
            else
            {
                ReleaseType = "feature";
                Major = tagMajor;
                Minor = tagMinor;
                Patch = tagPatch;
                CommitsAfterTag = tagCommits;
                Postfix = "." + tagCommits + "-" + ShortHash;
            }

            Version = ReleaseType + "-" + Major + "." + Minor + "." + Patch + Postfix;
            ShortVersion = Major + "." + Minor + "." + Patch;
            PlainVersion = ShortVersion + "." + CommitsAfterTag;
        }

        string ReadBranch(out int branchMajor, out int branchMinor, out int branchPatch)
        {
            var branch = ReadBranch();
            int commit;
            ExtractVersion(branch, out branchMajor, out branchMinor, out branchPatch, out commit);
            return branch;
        }

        string ReadBranch()
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
		
		private void GetLastCommitInformation()
		{
            string output;
            string error;
			
			string format = "Hash:%H%n" + 
							"ShortHash:%h%n";
			
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

        public string ReadTagVersion(out int major, out int minor, out int patch, out int commit)
        {   
            var version = GetTagInformation();
            Console.WriteLine(version);
            
            ExtractVersion(version, out major, out minor, out patch, out commit);
            return null;
        }

        public void ExtractVersion(string version, out int major, out int minor, out int patch, out int commit)
        {
            var reg = new Regex("^(([a-z]+)-)?([0-9]+)\\.([0-9]+)([^0-9a-zA-Z]([0-9]+))?([^0-9a-zA-Z]([0-9]+))?");
            var match = reg.Match(version);

            if (match.Success)
            {
                // var buildType = match.Groups[2].Value;
                int.TryParse(match.Groups[3].Value, out major);
                int.TryParse(match.Groups[4].Value, out minor);
                int.TryParse(match.Groups[6].Value, out patch);
                int.TryParse(match.Groups[8].Value, out commit);
            }
            else
            {
                major = 0;
                minor = 0;
                patch = 0;
                commit = 0;
            }
        }
		
		private string GetTagInformation()
		{
            string output;
            string error;
			
            int returnCode = Tools.CommandLineExecute("git", "describe --tags --long", out output, out error);

		    if (returnCode != 0 || string.IsNullOrWhiteSpace(output))
		    {
                var commits = 0;
                returnCode = Tools.CommandLineExecute("git", "rev-list --count HEAD", out output, out error);
                if (returnCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    int.TryParse(output, out commits);
                }

                Console.WriteLine("Failed to get tag information. Possible causes: No tag has been set yet; Git is not installed or not in PATH; This is not a git repository.");
		        output = "dev-0.0.0-" + commits + "-g" + ShortHash;
		    }

            return output;
		}
    }
}
