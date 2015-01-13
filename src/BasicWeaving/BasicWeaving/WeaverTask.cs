using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BasicWeaving
{
    public class WeaverTask : Task
    {
        public string TargetPath { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(this.TargetPath) || !File.Exists(this.TargetPath))
            {
                this.Log.LogError("Assembly {0} not found. Please provide a valid assembly path.", this.TargetPath);
                return false;
            }

            this.Log.LogMessage(MessageImportance.Low, "Starting weaver for {0} assembly.", this.TargetPath);

            var weaver = new Weaver();
            weaver.Execute(this.TargetPath);

            return true;
        }
    }
}