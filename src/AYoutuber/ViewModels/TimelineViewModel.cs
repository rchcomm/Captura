using Captura.Models;
using Captura.Models.VideoItems;
using Captura.ViewModels;
using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Captura
{
    public class TimelineViewModel : ViewModelBase
    {
        #region Properties
        public int ScaleWidth { get; set; } = 940;
        public int ScaleHeight { get; set; } = 530;

        public Window OwnerWindow { get; set; }

        /// <summary>
        /// 현재 작업 번호
        /// </summary>
        public int WorkNumber { get; private set; } = 1;

        /// <summary>
        /// 캡춰 경로
        /// </summary>
        public string OutPath { get; private set; } = string.Empty;

        public string OutRelativePath { get; set; }

        public string ResultPath { get; set; }

        public ObservableCollection<MediaItem> MediaCollection { get; set; } = new ObservableCollection<MediaItem>();

        public string ApplicationBaseDirectory { get; set; }

        public string OutVideoFileName { get; set; } = "Result.mp4";
        public string OutVideoSubTitleFileName { get; set; }
        #endregion

        #region Commands
        /// <summary>
        /// 미리보기 Command
        /// </summary>
        public DelegateCommand MakePreviewVideoCommand { get; set; }

        public DelegateCommand EditSubtitleCommand { get; set; }

        public DelegateCommand NextCommand { get; set; }

        public DelegateCommand PlayMediaCommand { get; set; }

        public DelegateCommand StopMediaCommand { get; set; }
        #endregion

        //public TimelineViewModel()
        //    : this(1, "")
        //{

        //}

        public TimelineViewModel(int workNumber, string outPath)
        {
            this.Initialization(workNumber, outPath);
        }

        public void Initialization(int workNumber, string outPath)
        {
            this.WorkNumber = workNumber;
            this.OutPath = outPath;
            this.ApplicationBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            this.OutRelativePath = "";

            this.ResultPath = Path.Combine(outPath, "Result");
            this.OutVideoSubTitleFileName = Path.Combine(this.ResultPath, "Result.srt");

            if (!Directory.Exists(this.ResultPath))
            {
                Directory.CreateDirectory(this.ResultPath);
            }

            //if(System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                int fileIndex = 0;
                foreach (var filePath in Directory.GetFiles(outPath))
                {
                    var fileInfo = new FileInfo(filePath);

                    switch (fileInfo.Extension.ToLower())
                    {
                        case ".png":
                        case ".gif":
                            fileIndex++;
                            this.MediaCollection.Add(new MediaItem(this) { FileName = fileInfo.Name, Id = fileIndex, MediaType = MediaType.Image, Order = fileIndex, Interval = 5 });
                            break;
                        case ".mp4":
                            fileIndex++;
                            this.MediaCollection.Add(new MediaItem(this) { FileName = fileInfo.Name, Id = fileIndex, MediaType = MediaType.Video, Order = fileIndex, Interval = 5 });
                            break;
                    }
                }
            }

            this.PlayMediaCommand = new DelegateCommand((obj) => {
                var mediaElement = obj as MediaElement;
                if (mediaElement != null)
                {

                }
            });

            this.MediaTargetSizes = new ObservableCollection<string>(Enum.GetNames(typeof(RegionSize)));

            this.MakePreviewVideoCommand = new DelegateCommand(() => {
                var imageSequenceFileName = Path.Combine(this.OutPath, "Temp", "imageSequence.txt");
                var imageSequenceFileNameArg = this.GetWorkRelativePath() + "Temp/" + "imageSequence.txt";
                var outVideoFilePath = Path.Combine(this.ResultPath, this.OutVideoFileName);
                var outVideoFilePathArg = this.GetWorkRelativePath() + "Result/" + this.OutVideoFileName;
                var bgmNameArg = "../Bgm/" + "Awakening.mp3"; //../BGM/Silver.mp3

                if (File.Exists(imageSequenceFileName))
                {
                    File.Delete(imageSequenceFileName);
                }

                if (File.Exists(outVideoFilePath))
                {
                    File.Delete(outVideoFilePath);
                }

                // 자막 파일 존재 여부 체크
                if (!File.Exists(this.OutVideoSubTitleFileName))
                {
                    // 빈 자막 파일을 만든다.
                    using (var sw = File.CreateText(this.OutVideoSubTitleFileName))
                    {
                        sw.WriteLine("1");
                        sw.WriteLine("00:00:00,000 --> 00:00:01,000");
                    }
                }

                var tempOutPath = Path.Combine(this.OutPath, "Temp");
                var outTempVideoFiles = this.ConvertImageToVideoGenerate(this.MediaCollection, tempOutPath);

                FileInfo fileInfo = new FileInfo(imageSequenceFileName);
                using (var sw = fileInfo.CreateText())
                {
                    foreach (var videoFile in outTempVideoFiles)
                    {
                        sw.WriteLine("file '" + videoFile + "'");
                    }
                    sw.Flush();
                }

                VideoGenerate(imageSequenceFileNameArg, bgmNameArg, outVideoFilePathArg, outVideoFilePath, true);

                Directory.Delete(tempOutPath, true);
            });

            this.EditSubtitleCommand = new DelegateCommand(() => {
                ProcessStartInfo editStartInfo = new ProcessStartInfo();
                var outVideoPath = Path.Combine(this.ResultPath, this.OutVideoFileName);
                if (File.Exists(outVideoPath))
                {
                    var arguments = string.Format("{0} {1} {2}", Path.Combine(this.ResultPath, this.OutVideoSubTitleFileName), outVideoPath, "ko-KR");
                    var workingDirectory = Path.Combine(this.ApplicationBaseDirectory);
                    var processStartInfo = new ProcessStartInfo(Path.Combine(workingDirectory, "SubtitleEdit.exe"))
                    {
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        WindowStyle = ProcessWindowStyle.Normal,
                    };

                    try
                    {
                        using (var process = new Process())
                        {
                            process.StartInfo = processStartInfo;
                            process.Start();
                            process.WaitForExit();
                        }
                    }
                    catch (Exception ex)
                    {
                        var message = ex.Message;
                    }
                }
                else
                {
                    MessageBox.Show("동영상 파일이 없습니다. \"Preview\" 버튼을 눌러서 확인해 주세요!");
                }
            });

            this.NextCommand = new DelegateCommand(() => {
                ModernDialog.ShowMessage("NextCommand", "title", MessageBoxButton.OK, this.OwnerWindow);
            });
        }

        /// <summary>
        /// 비디오 생성
        /// </summary>
        /// <param name="imageSequenceFileNameArg">sequence txt 상대 파일 위치 - ../Works/Work_0001/xxxxx.txt</param>
        /// <param name="bgmNameArg">음악파일 상대 위치 - ../BGM/xxxx.mp3</param>
        /// <param name="outVideoFilePathArg">출력파일 상대 위치 - ../Works/Work_0001/Result/xxxxx.mp4</param>
        /// <param name="outVideoFilePath">출력파일 절대 위치 - D:\</param>
        /// <param name="isPlay">마지막에 FFPlay로 플레이 여부</param>
        public void VideoGenerate(string imageSequenceFileNameArg, string bgmNameArg, string outVideoFilePathArg, string outVideoFilePath, bool isPlay = true)
        {
            if (string.IsNullOrEmpty(bgmNameArg))
            {
                bgmNameArg = string.Empty;
            }
            else
            {
                bgmNameArg = "-i " + bgmNameArg;
            }

            this.SetSacleWidthHeight(this.MediaTargetSizeSelectedItem);
            var argument = string.Format(" -f concat -i {0} {1} -r 30 -pix_fmt yuv420p -vf scale={3}:{4} -c:a copy -shortest -vsync vfr {2}", imageSequenceFileNameArg, bgmNameArg, outVideoFilePathArg, this.ScaleWidth, this.ScaleHeight);
            this.VideoGenerateExecute(argument, isPlay, outVideoFilePath);
        }

        private void VideoGenerateExecute(string arguments, bool isPlay = false, string outVideoFilePath = null)
        {
            var workingDirectory = Path.Combine(this.ApplicationBaseDirectory, "Modules");
            var processStartInfo = new ProcessStartInfo(Path.Combine(workingDirectory, "ffmpeg.exe"))
            {
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message;
            }

            if (isPlay)
            {
                processStartInfo = new ProcessStartInfo(Path.Combine(workingDirectory, "ffplay.exe"))
                {
                    Arguments = outVideoFilePath,
                    WorkingDirectory = workingDirectory,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                };

                try
                {
                    var process = Process.Start(processStartInfo);
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                }
            }
        }

        public List<string> ConvertImageToVideoGenerate(IEnumerable<MediaItem> sources, string tempOutPath)
        {
            var tempVideoFiles = new List<string>();

            if (Directory.Exists(tempOutPath))
            {
                Directory.Delete(tempOutPath, true);
            }
            Directory.CreateDirectory(tempOutPath);

            int tempIndex = 0;
            foreach (var source in sources)
            {
                tempIndex++;
                var outVideoFileName = "TempVideo_" + tempIndex.ToString().PadLeft(4, '0') + ".mp4";
                var outVideoFilePath = Path.Combine(this.OutPath, "Temp", outVideoFileName); ;
                string imageSequenceFileName = "TempVideo_" + tempIndex.ToString().PadLeft(4, '0') + "imageSequence.txt";
                string imageSequenceFileNameArg = this.GetWorkRelativePath() + imageSequenceFileName;
                string outVideoFilePathArg = this.GetWorkRelativePath() + "Temp/" + outVideoFileName;
                tempVideoFiles.Add(outVideoFileName);

                switch (source.MediaType)
                {
                    case MediaType.AnimationGIF:
                        break;
                    case MediaType.Image:
                        var sequenceFilePath = Path.Combine(this.OutPath, imageSequenceFileName);
                        if (File.Exists(sequenceFilePath)) File.Delete(sequenceFilePath);
                        FileInfo fileInfo = new FileInfo(sequenceFilePath);
                        using (var sw = fileInfo.CreateText())
                        {
                            for (int i = 0; i <= source.Interval; i++)
                            {
                                sw.WriteLine("file '" + Path.GetFileName(source.FileName) + "'");
                                sw.WriteLine("duration 1");
                            }
                        }

                        VideoGenerate(imageSequenceFileNameArg, null, outVideoFilePathArg, outVideoFilePath, false);
                        fileInfo.Delete();
                        break;
                    case MediaType.Video:
                        var inputVideoFilePathArg = this.GetWorkRelativePath() + source.FileName;
                        this.SetSacleWidthHeight(this.MediaTargetSizeSelectedItem);
                        var argument = string.Format(" -i {0} -r 30 -pix_fmt yuv420p -vf scale={2}:{3} -c:a copy -shortest -vsync vfr {1}", inputVideoFilePathArg, outVideoFilePathArg, this.ScaleWidth, this.ScaleHeight);

                        this.VideoGenerateExecute(argument);
                        //File.Copy(source.MediaSource, outVideoFilePath);
                        break;
                }
            }

            return tempVideoFiles;
        }

        public void SetSacleWidthHeight(string selectedSacleSizeString)
        {
            var dimension = selectedSacleSizeString.Split('_');
            if (dimension.Length == 3)
            {
                this.ScaleWidth = int.Parse(dimension[1]);
                this.ScaleHeight = int.Parse(dimension[2]);
            }
        }

        public ObservableCollection<string> MediaTargetSizes { get; set; }

        public string MediaTargetSizeSelectedItem { get; set; } = "YOUTUBE_940_530";

        public string GetWorkRelativePath()
        {
            return "../Works/Work_" + Environment.UserName + "_" + this.WorkNumber.ToString().PadLeft(4, '0') + "/";
        }
    }
}
