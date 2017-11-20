using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Captura.ViewModels
{
    public class WorkViewModel : ViewModelBase
    {
        #region Properties
        public MainViewModel MainViewModel { get; private set; }
        #endregion

        #region Commands
        /// <summary>
        /// New Work Command
        /// </summary>
        public DelegateCommand NewWorkNumberCommand { get; set; }

        /// <summary>
        /// Edit Work Command
        /// </summary>
        public DelegateCommand EditWorkNumberCommand { get; set; }

        public DelegateCommand WorkNumberSelectionChangedCommand { get; set; }

        public ObservableCollection<int> WorkNumbers { get; set; }

        private int selectedWorkNumber;

        public int SelectedWorkNumber
        {
            get { return selectedWorkNumber; }
            set
            {
                selectedWorkNumber = value;
                this.OnPropertyChanged();
            }
        }

        #endregion

        public WorkViewModel(MainViewModel mainViewModel)
        {
            this.MainViewModel = mainViewModel;
        }
    }
}
