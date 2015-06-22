using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PCBInspection.Model;
using System.ComponentModel;
using System.Windows.Input;
using PCBInspection.View;

namespace PCBInspection.ViewModel
{
    class ImageProcessorViewModel : INotifyPropertyChanged
    {
        public ImageProcessor ImageProcessor { get; private set; }

        public ImageProcessorViewModel(ImageProcessor imageProcessor)
        {
            this.ImageProcessor = imageProcessor;
        }

        public string ModelImagePath
        {
            get { return this.ImageProcessor.ModelImagePath; }
            set { this.ImageProcessor.ModelImagePath = value; OnPropertyChanged("ModelImagePath"); }
        }

        #region ResetModelImage Command
        private ICommand resetModelImageCommand;
        public ICommand ResetModelImageCommand
        {
            get
            {
                if (resetModelImageCommand == null)
                {
                    resetModelImageCommand = new RelayCommand((e) => OnResetModelImage(), (e) => OnCanResetModelImage(e));
                }
                return resetModelImageCommand;
            }
        }

        private bool OnCanResetModelImage(object e)
        {
            return true;
        }

        private void OnResetModelImage()
        {
            this.ResetModelImageFile();
            this.ImageProcessor.LoadModelImage();
        }
        #endregion

        #region SelectModelImage Command
        private ICommand selectModelImageCommand;
        public ICommand SelectModelImageCommand
        {
            get
            {
                if (selectModelImageCommand == null)
                {
                    selectModelImageCommand = new RelayCommand((e) => OnSelectModelImage(), (e) => OnCanSelectModelImage(e));
                }
                return selectModelImageCommand;
            }
        }

        private bool OnCanSelectModelImage(object e)
        {
            return true;
        }

        private void OnSelectModelImage()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".bmp";

            dlg.Filter = "Bmp Image (.bmp)|*.bmp|Png Image (.png)|*.png|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                this.ModelImagePath = dlg.FileName;
                this.ImageProcessor.LoadModelImage();
            }
        }
        #endregion

        #region SelectRoi Command
        private ICommand selectRoiCommand;
        public ICommand SelectRoiCommand
        {
            get
            {
                if (selectRoiCommand == null)
                {
                    selectRoiCommand = new RelayCommand((e) => OnSelectRoiCamera(), (e) => OnCanSelectRoiCamera(e));
                }
                return selectRoiCommand;
            }
        }
        private bool OnCanSelectRoiCamera(object e)
        {
            return true;
        }
        private void OnSelectRoiCamera()
        {
            if (this.ImageProcessor.LoadModelImage())
            {
                this.ImageProcessor.SelectROI();
            }
        }
        #endregion

        #region FindIcs Command
        private ICommand findIcsCommand;
        public ICommand FindIcsCommand
        {
            get
            {
                if (findIcsCommand == null)
                {
                    findIcsCommand = new RelayCommand((e) => OnFindIcsCamera(), (e) => OnCanFindIcsCamera(e));
                }
                return findIcsCommand;
            }
        }
        private bool OnCanFindIcsCamera(object e)
        {
            return true;
        }
        private void OnFindIcsCamera()
        {
            if (this.ImageProcessor.LoadModelImage())
            {
                if (this.ImageProcessor.SelectROI())
                {
                    this.ImageProcessor.FindTargetICs();
                }
            }
        }
        #endregion

        #region SelectIcsCommand
        private ICommand selectIcsCommand;
        public ICommand SelectIcsCommand
        {
            get
            {
                if (selectIcsCommand == null)
                {
                    selectIcsCommand = new RelayCommand((e) => OnSelectIcsCamera(), (e) => OnCanSelectIcsCamera(e));
                }
                return selectIcsCommand;
            }
        }
        private bool OnCanSelectIcsCamera(object e)
        {
            return true;
        }
        private void OnSelectIcsCamera()
        {
            if (this.ImageProcessor.LoadModelImage())
            {
                if (this.ImageProcessor.SelectROI())
                {
                    if (this.ImageProcessor.FindTargetICs())
                    {
                        this.ImageProcessor.SelectTargetICs();
                    }
                }
            }
        }
        #endregion

        #region SaveModelFile Command
        private ICommand saveModelFileCommand;
        public ICommand SaveModelFileCommand
        {
            get
            {
                if (saveModelFileCommand == null)
                {
                    saveModelFileCommand = new RelayCommand((e) => OnSaveModelFileCamera(), (e) => OnCanSaveModelFileCamera(e));
                }
                return saveModelFileCommand;
            }
        }
        private bool OnCanSaveModelFileCamera(object e)
        {
            return true;
        }
        private void OnSaveModelFileCamera()
        {
            if (this.ImageProcessor.LoadModelImage())
            {
                if (this.ImageProcessor.SelectROI())
                {
                    if (this.ImageProcessor.FindTargetICs())
                    {
                        if (this.ImageProcessor.SelectTargetICs())
                        {
                            this.ImageProcessor.MakeModelFile();
                        }
                    }
                }
            }
        }
        #endregion

        #region Detect Command
        private ICommand detectCommand;
        public ICommand DetectCommand
        {
            get
            {
                if (detectCommand == null)
                {
                    detectCommand = new RelayCommand((e) => OnDetectCamera(), (e) => OnCanDetectCamera(e));
                }
                return detectCommand;
            }
        }
        private bool OnCanDetectCamera(object e)
        {
            return true;
        }

        private void OnDetectCamera()
        {
            if (this.ImageProcessor.LoadModelImage())
            {
                if (this.ImageProcessor.SelectROI())
                {
                    if (this.ImageProcessor.FindTargetICs())
                    {
                        if (this.ImageProcessor.SelectTargetICs())
                        {
                            if (HasValidModelFile())
                            {
                                if (this.ImageProcessor.CompareICRegions())
                                {
                                    System.Windows.MessageBox.Show("合格");
                                }
                                else
                                {
                                    System.Windows.MessageBox.Show("不合格");
                                }
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("没有找到模板文件");
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public bool HasValidModelFile()
        {
            return this.ImageProcessor.LoadModelFile();
        }

        public bool ShowImage()
        {
            return this.ImageProcessor.LoadImage();
        }

        public bool Detect()
        {
            if (this.ImageProcessor.LoadImage())
            {
                if (this.ImageProcessor.SelectROI())
                {
                    if (this.ImageProcessor.FindTargetICs())
                    {
                        if (this.ImageProcessor.SelectTargetICs())
                        {
                            if (HasValidModelFile())
                            {
                                if (this.ImageProcessor.CompareICRegions())
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void ResetModelImageFile()
        {
            this.ImageProcessor.ResetModelImageFile();
            OnPropertyChanged("ModelImagePath");
        }
    }
}
