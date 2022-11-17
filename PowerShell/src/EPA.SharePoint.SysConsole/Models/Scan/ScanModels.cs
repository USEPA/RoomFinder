using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScanModels
    {
        public ScanModels()
        {
            this.ObjectType = AddInObjectTypeEnum.File;
            this.Timestamp = DateTimeOffset.Now;
            this.ViolationLines = new List<ScanLogLine>();
            this.EvaluationLines = new List<ScanLogLine>();
            this.PermissionLines = new List<ScanLogLine>();
            NumberOfLines = 0;
        }

        public ScanModels(AddInObjectTypeEnum fileType) : this()
        {
            this.ObjectType = fileType;
        }

        public ScanModels(AddInObjectTypeEnum fileType, string relativeUrl) : this(fileType)
        {
            this.ObjectUrl = relativeUrl;
        }

        public AddInObjectTypeEnum ObjectType { get; set; }


        public string ObjectUrl { get; set; }

        public DateTimeOffset Timestamp { get; set; }


        public bool IsHttps { get; set; }

        public bool IsExcluded { get; set; }

        public int NumberOfLines { get; set; }

        /// <summary>
        /// The scan determined there are results that do not align with Governance
        /// </summary>
        public bool Violation { get; set; }

        public IList<ScanLogLine> ViolationLines { get; set; }

        public bool SetViolationLines(int lineNumber, string result)
        {
            if (!ViolationLines.Any(a => a.LineNumber == lineNumber && a.Result.IndexOf(result) > -1))
            {
                Violation = true;
                ViolationLines.Add(new ScanLogLine(lineNumber, result));
                return true;
            }
            return false;
        }


        /// <summary>
        /// The scan determined there are results that need to be reviewed by a person
        /// </summary>
        public bool Evaluation { get; set; }

        public IList<ScanLogLine> EvaluationLines { get; set; }

        public bool SetEvaluationLines(int lineNumber, string result)
        {
            if (!EvaluationLines.Any(a => a.LineNumber == lineNumber && a.Result.IndexOf(result) > -1))
            {
                Evaluation = true;
                EvaluationLines.Add(new ScanLogLine(lineNumber, result));
                return true;
            }
            return false;
        }


        /// <summary>
        /// The scan deteremined there are flagged permission entries
        /// </summary>
        public bool Permission { get; set; }

        /// <summary>
        /// The log entries for scanning workflow
        /// </summary>
        public IList<ScanLogLine> PermissionLines { get; set; }

        public bool SetPermissionLines(int lineNumber, string result)
        {
            if (!PermissionLines.Any(a => a.LineNumber == lineNumber && a.Result.IndexOf(result) > -1))
            {
                Permission = true;
                PermissionLines.Add(new ScanLogLine(lineNumber, result));
                return true;
            }
            return false;
        }




        public override string ToString()
        {
            if (Violation || Evaluation || Permission)
            {
                var result = string.Format("{0};{1}", ObjectType.ToString("f"), ObjectUrl);
                if (Violation || ViolationLines.Any())
                    result += string.Format(";Violations:{0}", string.Join("||", ViolationLines.Select(s => s.ToString())));
                if (Evaluation || EvaluationLines.Any())
                    result += string.Format(";Evaluations:{0}", string.Join("||", EvaluationLines.Select(s => s.ToString())));
                if (Permission || PermissionLines.Any())
                    result += string.Format(";Permissions:{0}", string.Join("||", PermissionLines.Select(s => s.ToString())));

                return result;
            }
            else if (IsExcluded)
            {
                return string.Format("Excluded: => {0}", ObjectUrl);
            }
            else
            {
                return string.Format("Clean: => {0}", ObjectUrl);
            }
        }
    }
}
