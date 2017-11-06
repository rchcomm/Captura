using Captura.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Captura.Models
{
    public class MediaItem : ViewModelBase
    {
        #region Properties
        [JsonIgnore]
        public TimelineViewModel ViewModel { get; private set; }

        private int id;

        public int Id
        {
            get { return id; }
            set
            {
                id = value;
            }
        }

        private string fileName;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        private MediaType mediaType;

        public MediaType MediaType
        {
            get { return mediaType; }
            set
            {
                mediaType = value;
            }
        }

        private int order;

        public int Order
        {
            get { return order; }
            set
            {
                order = value;
                this.OnPropertyChanged();
            }
        }

        private bool isBGMOverlap = true;

        public bool IsBGMOverlap
        {
            get { return isBGMOverlap; }
            set
            {
                isBGMOverlap = value;
                this.OnPropertyChanged();
            }
        }


        private int interval;

        public int Interval
        {
            get { return interval; }
            set
            {
                if(interval != value)
                {
                    interval = value;
                    this.OnPropertyChanged();
                }                
            }
        }

        [JsonIgnore]
        public string MediaSource
        {
            get
            {
                return System.IO.Path.Combine(this.ViewModel.OutPath, FileName);
            }
        }

        private string notice = string.Empty;

        public string Notice
        {
            get { return notice; }
            set { notice = value; }
        }

        private TimeSpan startRange = new TimeSpan();

        /// <summary>
        /// 동영상 시작 시간
        /// </summary>
        public TimeSpan StartRange
        {
            get { return startRange; }
            set
            {
                startRange = value;
                this.OnPropertyChanged();
            }
        }


        private TimeSpan endRange = new TimeSpan(0, 10, 10, 10, 1000);

        /// <summary>
        /// 동영상 종료 시간
        /// </summary>
        public TimeSpan EndRange
        {
            get { return endRange; }
            set
            {
                endRange = value;
                this.OnPropertyChanged();
            }
        }

        private TimeSpan endTime = new TimeSpan(1, 10, 10, 10, 1000);

        /// <summary>
        /// 동영상 재생 종료 시간
        /// </summary>
        public TimeSpan EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }

        #endregion

        #region Construction
        public MediaItem(TimelineViewModel timelineViewModel)
        {
            this.ViewModel = timelineViewModel;
        }
        #endregion
    }
}
