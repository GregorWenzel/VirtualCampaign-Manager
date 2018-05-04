using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;
using VirtualCampaign_Manager.Helpers;

namespace VirtualCampaign_Manager.Rendering
{
    public class CompositionModifier
    {
        Job job;
        string[] CompLines;
        private int saverCount = 0;

        public JobErrorStatus ErrorStatus = JobErrorStatus.JES_NONE;

        public CompositionModifier(Job job)
        {
            this.job = job;
        }

        public void Modify()
        {
            saverCount = ReadComposition();
            if (ErrorStatus == JobErrorStatus.JES_NONE)
            {
                ModifyComposition();
            }
            if (ErrorStatus == JobErrorStatus.JES_NONE)
            {
                WriteComposition();
            }
        }

        private int ReadComposition()
        {
            job.LogText("Reading composition file " + JobPathHelper.GetProductCompositionPath(job));

            if (!File.Exists(JobPathHelper.GetProductCompositionPath(job)))
            {
                ErrorStatus = JobErrorStatus.JES_COMP_MISSING;
                return -1;
            }
            CompLines = File.ReadAllLines(JobPathHelper.GetProductCompositionPath(job));
            return CompLines.Select(item => Regex.Matches(item, " = Saver {").Count).Sum();
        }

        private void ModifyComposition()
        {
            ModifyAllTools();

            int outputCounter = ModifyOutputs();
            int motifCounter = ModifyMotifs();

            if (job.CanReformat)
            {
                ModifyResizeTool();
            }

            if (motifCounter < job.MotifList.Count)
            {
                ErrorStatus = JobErrorStatus.JES_COMP_MOD_MOTIF_LOADERS;
                return;
            }
        }

        private void ModifyAllTools()
        {
            for (int i = 0; i < CompLines.Length; i++)
            {
                string line = CompLines[i];

                if (line.IndexOf(Settings.BasePathVariable) >= 0)
                {
                    line = line.Replace(@"\\", @"\");
                    line = line.Replace(Settings.BasePathVariable, Settings.LocalBasePath);
                    line = line.Replace(@"\", @"\\");
                    CompLines[i] = line;
                }
            }
        }

        private bool ModifyResizeTool()
        {
            FilmOutputFormat largestOutputFormat = job.Production.Film.LargestFilmOutputFormat;
            SetNumericValueInTool("reformat", "Width", " " + largestOutputFormat.Width.ToString() + ", }");
            SetNumericValueInTool("reformat", "Height", " " + largestOutputFormat.Height.ToString() + ", }");

            return true;
        }

        private int ModifyOutputs()
        {
            int result = 0;
            job.OutputExtension = GetOutputExtension();
            if (ErrorStatus != JobErrorStatus.JES_NONE) return -1;

            string outputPath;

            if (job.IsZip)
                outputPath = JobPathHelper.GetLocalJobRenderOutputPathForZip(job);
            else
                outputPath = JobPathHelper.GetLocalJobRenderOutputMask(job);

            if (SetValueInTool("OutputClips", "", outputPath))
                result++;

            int saverResult = 0;
            for (int i = 0; i < saverCount; i++)
            {
                bool subResult = false;

                if (job.IsZip)
                    subResult = SetValueInTool(string.Format("Saver{0}", i + 1), "Filename", outputPath, true);
                else
                    subResult = SetValueInTool(string.Format("Saver{0}", i + 1), "Filename", outputPath, false);

                if (subResult == true)
                    saverResult++;
            }

            if (saverResult > 0)
                result++;

            return result;
        }

        private int ModifyMotifs()
        {
            int result = 0;

            for (int i = 0; i < job.MotifList.Count; i++)
            {
                Motif motif = job.MotifList[i];

                if (motif.Type == "motif" || motif.Type == "film")
                {
                    if (motif.Type == "motif")
                    {
                        string targetFileName = ProductionPathHelper.GetProductionMotifPath(job.Production, motif);

                        SetValueInTool(motif.LoaderName, "Filename", targetFileName);
                        switch (motif.Extension)
                        {
                            case ".jpg":
                                if (SetValueInTool(motif.LoaderName, "FormatID", "JpegFormat"))
                                    result++;
                                break;
                            case ".png":
                                if (SetValueInTool(motif.LoaderName, "FormatID", "PNGFormat"))
                                    result++;
                                break;
                        }
                    }
                    else
                    {
                        bool success = false;
                        success = SetNumericValueInTool(motif.LoaderName, "Length ", motif.Frames.ToString());

                        if (!success)
                            success = SetNumericValueInTool(motif.LoaderName, "StartFrame", "1,\r\nLength = " + motif.Frames.ToString());
                        else
                            success = success && SetNumericValueInTool(motif.LoaderName, "StartFrame", "1");

                        string motifPath = ProductionPathHelper.GetProductionAnimatedMotifPath(job.Production, motif);

                        success = success && SetValueInTool(motif.LoaderName, "Filename", motifPath)
                            && SetValueInTool(motif.LoaderName, "FormatID", "TargaFormat")
                            && SetNumericValueInTool(motif.LoaderName, "TrimIn", "0")
                            && SetNumericValueInTool(motif.LoaderName, "TrimOut", (motif.Frames - 1).ToString())
                            && SetNumericValueInTool(motif.LoaderName, "GlobalStart", "0")
                            && SetNumericValueInTool(motif.LoaderName, "GlobalEnd", (motif.Frames - 1).ToString());

                        if (success)
                            result++;
                    }
                }
                else if (motif.Type == "text")
                {
                    if (SetMotifText(motif))
                        result++;

                }
            }
            return result;
        }

        private bool SetMotifText(Motif motif)
        {
            bool result = true;
            result = result & SetValueInTool(motif.LoaderName + "_Text", "StyledText = Input { Value", motif.Text);
            result = result & SetValueInTool(motif.LoaderName + "_Text3D", "StyledText = Input { Value", motif.Text);
            return result;
        }

        private string GetOutputExtension()
        {
            string result = "";
            for (int i = 0; i < CompLines.Length; i++)
                if (CompLines[i].Contains("OutputClips"))
                {
                    string line = CompLines[i + 1];
                    int start = line.LastIndexOf('.');
                    int end = line.LastIndexOf('\"');
                    if (start < 0 || end < 0)
                    {
                        ErrorStatus = JobErrorStatus.JES_COMP_MOD_OUTPUTS;
                        return "";
                    }
                    result = line.Substring(start + 1, end - start - 1);
                }
            return "." + result;
        }

        private void WriteComposition()
        {
            string path = JobPathHelper.GetJobCompPath(job);
            job.LogText(string.Format("Saving comp '{0}'", path));
            File.WriteAllLines(path, CompLines);
        }

        private bool SetValueInTool(string ToolName, string ParameterName, string value, bool keepFilename = false)
        {
            int start = -1;
            int end = -1;
            ToolName += " = ";

            job.LogText("-TOOL: " + ToolName + ", PARAMETER: " + ParameterName + " --> " + value);

            for (int i = 0; i < CompLines.Length; i++)
                if (CompLines[i].Contains(ToolName))
                {
                    start = i;
                    end = GetEndLine(start);
                    SetValue(start, end, ParameterName, value, keepFilename);
                    return true;
                }

            return false;
        }

        private void SetValue(int start, int end, string ParameterName, string value, bool keepFilename = false)
        {
            if (ParameterName.Length == 0)
                start += 1;

            for (int i = start; i < end; i++)
                if (CompLines[i].Contains(ParameterName))
                {
                    if (keepFilename == true)
                    {
                        string oldFilename = CompLines[i].Substring(CompLines[i].LastIndexOf("\\"));
                        oldFilename = oldFilename.Substring(1, oldFilename.Length - 3);
                        value = Path.Combine(value, oldFilename);
                    }
                    CompLines[i] = ExchangeValue(i, value).Replace(@"\", @"\\");
                }
        }

        private string ExchangeValue(int index, string value)
        {
            string result = CompLines[index];

            int start = result.IndexOf('\"');
            int end = result.LastIndexOf('\"');

            result = result.Remove(start + 1, end - start - 1);
            result = result.Insert(start + 1, value);

            return result;

        }

        private bool SetNumericValueInTool(string ToolName, string ParameterName, string value)
        {
            int start = -1;
            int end = -1;
            ToolName += " = ";

            job.LogText("-TOOL: " + ToolName + ", PARAMETER: " + ParameterName + " --> " + value);

            for (int i = 0; i < CompLines.Length; i++)
                if (CompLines[i].Contains(ToolName))
                {
                    start = i;
                    end = GetEndLine(start);
                    return SetNumericValue(start, end, ParameterName, value);
                }

            return false;
        }

        private bool SetNumericValue(int start, int end, string ParameterName, string value)
        {
            if (ParameterName.Length == 0)
                start += 1;

            for (int i = start; i < end; i++)
                if (CompLines[i].Contains(ParameterName))
                {
                    CompLines[i] = ExchangeNumericValue(i, value).Replace(@"\", @"\\");
                    return true;
                }

            return false;
        }

        private string ExchangeNumericValue(int index, string value)
        {
            string result = CompLines[index];

            int start = result.LastIndexOf('=');
            int end = result.LastIndexOf(',');

            result = result.Remove(start + 1, end - start - 1);
            result = result.Insert(start + 1, value);

            return result;

        }


        private int GetEndLine(int start)
        {
            int bracketCount = 1;
            int counter = start;

            while (bracketCount > 0)
            {
                counter++;
                if (CompLines[counter].Contains("{"))
                    bracketCount++;
                if (CompLines[counter].Contains("}"))
                    bracketCount--;
            }

            return counter;
        }

        private string IntToLetters(int value)
        {
            string result = string.Empty;
            while (--value >= 0)
            {
                result = (char)('A' + value % 26) + result;
                value /= 26;
            }
            return result;
        }
    }
}