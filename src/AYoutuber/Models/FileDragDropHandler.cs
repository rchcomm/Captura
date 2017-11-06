using Captura;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Captura
{
    public class FileDragDropHandler : IDropTarget
    {
        private readonly TimelineViewModel viewModel;

        public FileDragDropHandler(TimelineViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();
            dropInfo.Effects = dragFileList.Count() > 0 ? DragDropEffects.Copy : DragDropEffects.None;
        }

        public void Drop(IDropInfo dropInfo)
        {
            var insertIndex = dropInfo.InsertIndex;
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();

            foreach (var filePath in dragFileList)
            {
                // file copy 
                var fileName = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + Path.GetExtension(filePath);
                var addedFilePath = Path.Combine(viewModel.OutPath, fileName);
                File.Copy(filePath, addedFilePath);

                viewModel.AddMediaCollection(addedFilePath, ++viewModel.LastestFileIndex, insertIndex);
                Thread.Sleep(1000);
            }

            viewModel.SaveGenerateInfo();
        }
    }
}
