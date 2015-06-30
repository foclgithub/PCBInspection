using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PCBInspection.Model;
using System.ComponentModel;
using System.Windows.Input;
using PCBInspection.View;
using System.Windows;
using System.Collections.ObjectModel;

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

        public bool DetectOld()
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

        public bool Detect()
        {
            if (this.ImageProcessor.LoadImage())
            {
                return this.ImageProcessor.Detect();
            }
            return false;
        }

        public bool LoadICModels()
        {
            return this.ImageProcessor.LoadICModels();
        }

        public void ResetModelImageFile()
        {
            this.ImageProcessor.ResetModelImageFile();
            OnPropertyChanged("ModelImagePath");
        }
   
        public bool SimulateDetect()
        {
            if (!SimulateLoadImage())
            {
                return false;
            }

            if (this.ImageProcessor.LoadImage(currentImage))
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

        private string currentImage = null;

        public bool SimulateLoadImage()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".png";

            dlg.Filter = "Png Image (.png)|*.png|Bmp Image (.bmp)|*.bmp|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                this.currentImage = dlg.FileName;
                return true;
            }
            return false;
        }

        #region DrawICROICommand
        private ICommand drawICROICommand;
        public ICommand DrawICROICommand
        {
            get
            {
                if (drawICROICommand == null)
                {
                    drawICROICommand = new RelayCommand((e) => OnDrawICROI(), (e) => OnCanDrawICROI(e));
                }
                return drawICROICommand;
            }
        }

        private bool OnCanDrawICROI(object e)
        {
            return true;
        }
        private void OnDrawICROI()
        {
            this.CurrentModelIC = this.ImageProcessor.DrawRectangle1();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnPropertyChanged("ModelICs");
                }), null);
        }
        #endregion

        public ObservableCollection<ICModelParameter> ModelICs
        {
            get
            { 
                return this.ImageProcessor.ModelICs; 
            }
        }

        private ICModelParameter currentModelIC;

        public ICModelParameter CurrentModelIC
        {
            get { return currentModelIC; }
            set { currentModelIC = value; OnPropertyChanged("CurrentModelIC"); }
        }

        #region ProcessICROICommand
        private ICommand processICROICommand;
        public ICommand ProcessICROICommand
        {
            get
            {
                if (processICROICommand == null)
                {
                    processICROICommand = new RelayCommand((e) => OnProcessOneICROI(), (e) => OnCanProcessOneICROI(e));
                }
                return processICROICommand;
            }
        }

        private bool OnCanProcessOneICROI(object e)
        {
            return this.CurrentModelIC != null;
        }
        private void OnProcessOneICROI()
        {
            if (this.CurrentModelIC != null)
            {
                this.ImageProcessor.ProcessOneIC(this.CurrentModelIC);
            }
        }
        #endregion

        #region ProcessConnectionROICommand
        private ICommand processConnectionROICommand;
        public ICommand ProcessConnectionROICommand
        {
            get
            {
                if (processConnectionROICommand == null)
                {
                    processConnectionROICommand = new RelayCommand((e) => OnProcessConnectionROI(), (e) => OnCanProcessConnectionROI(e));
                }
                return processConnectionROICommand;
            }
        }

        private bool OnCanProcessConnectionROI(object e)
        {
            return this.CurrentModelIC != null;
        }
        private void OnProcessConnectionROI()
        {
            if (this.CurrentModelIC != null)
            {
                string regInfo;
                if (this.ImageProcessor.ProcessConnectionROI(this.CurrentModelIC, out regInfo))
                {
                    this.RegionInfo = regInfo;
                }
            }
        }

        private string regionInfo;
        public string RegionInfo
        {
            get { return regionInfo; }
            set { regionInfo = value; OnPropertyChanged("RegionInfo"); }
        }

        #endregion

        #region ThresholdICROICommand
        private ICommand thresholdICROICommand;
        public ICommand ThresholdICROICommand
        {
            get
            {
                if (thresholdICROICommand == null)
                {
                    thresholdICROICommand = new RelayCommand((e) => OnThresholdOneICROI(), (e) => OnCanThresholdOneICROI(e));
                }
                return thresholdICROICommand;
            }
        }

        private bool OnCanThresholdOneICROI(object e)
        {
            return this.CurrentModelIC != null;
        }
        private void OnThresholdOneICROI()
        {
            if (this.CurrentModelIC != null)
            {
                this.ImageProcessor.ThresholdOneIC(this.CurrentModelIC);
            }
        }
        #endregion


        #region DetectCurrentICModelCommand
        private ICommand detectCurrentICModelCommand;
        public ICommand DetectCurrentICModelCommand
        {
            get
            {
                if (detectCurrentICModelCommand == null)
                {
                    detectCurrentICModelCommand = new RelayCommand((e) => OnDetectCurrentICModel(), (e) => OnCanDetectCurrentICModel(e));
                }
                return detectCurrentICModelCommand;
            }
        }

        private bool OnCanDetectCurrentICModel(object e)
        {
            return this.CurrentModelIC != null;
        }
        private void OnDetectCurrentICModel()
        {
            if (this.CurrentModelIC != null)
            {
                if (MessageBox.Show("确定删除当前芯片模板？", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    this.ImageProcessor.ModelICs.Remove(this.CurrentModelIC);
                    this.CurrentModelIC = null;
                }
            }
        }
        #endregion 

        #region DetectALLICModelCommand
        private ICommand detectALLICModelCommand;
        public ICommand DetectALLICModelCommand
        {
            get
            {
                if (detectALLICModelCommand == null)
                {
                    detectALLICModelCommand = new RelayCommand((e) => OnDetectALLICModel(), (e) => OnCanDetectALLICModel(e));
                }
                return detectALLICModelCommand;
            }
        }

        private bool OnCanDetectALLICModel(object e)
        {
            return true;
        }
        private void OnDetectALLICModel()
        {
            if (this.CurrentModelIC != null)
            {
                if (MessageBox.Show("确定清空所有芯片模板？", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    this.ModelICs.Clear();
                    this.CurrentModelIC = null;
                }
            }
        }
        #endregion 
        
        #region SaveICModelsCommand
        private ICommand saveICModelsCommand;
        public ICommand SaveICModelsCommand
        {
            get
            {
                if (saveICModelsCommand == null)
                {
                    saveICModelsCommand = new RelayCommand((e) => OnSaveICModels(), (e) => OnCanSaveICModels(e));
                }
                return saveICModelsCommand;
            }
        }

        private bool OnCanSaveICModels(object e)
        {
            return this.ModelICs != null && this.ModelICs.Count > 0;
        }
        private void OnSaveICModels()
        {
            if (this.ModelICs != null && this.ModelICs.Count > 0)
            {
                this.ImageProcessor.SaveICModels();
            }
            else
            {
                MessageBox.Show("没有芯片模板数据！", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region LoadICModelsCommand
        private ICommand loadICModelsCommand;
        public ICommand LoadICModelsCommand
        {
            get
            {
                if (loadICModelsCommand == null)
                {
                    loadICModelsCommand = new RelayCommand((e) => OnLoadICModels(), (e) => OnCanLoadICModels(e));
                }
                return loadICModelsCommand;
            }
        }

        private bool OnCanLoadICModels(object e)
        {
            return true;
        }
        private void OnLoadICModels()
        {
            if (!this.ImageProcessor.LoadICModels())
            {
                MessageBox.Show("加载芯片模板数据是失败！", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region DisplayALLICModelCommand
        private ICommand displayALLICModelCommand;
        public ICommand DisplayALLICModelCommand
        {
            get
            {
                if (displayALLICModelCommand == null)
                {
                    displayALLICModelCommand = new RelayCommand((e) => OnDispkayAllICModels(), (e) => OnCanDisplayAllICModels(e));
                }
                return displayALLICModelCommand;
            }
        }

        private bool OnCanDisplayAllICModels(object e)
        {
            return this.ModelICs != null && this.ModelICs.Count > 0;
        }
        private void OnDispkayAllICModels()
        {
            this.ImageProcessor.DisplayAllICModels();
        }
        #endregion

    }
}
