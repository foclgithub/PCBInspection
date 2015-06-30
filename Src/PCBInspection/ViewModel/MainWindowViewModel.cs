using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FactoryHandle = System.IntPtr;
using DeviceHandle = System.IntPtr;
using PCBInspection.Model;
using HalconDotNet;
using System.Windows.Input;
using PCBInspection.View;
using System.Windows;

namespace PCBInspection.ViewModel
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private IntPtr cameraViewerHandle;
        private HWindow imageViewerHandler;
        private Window mainWindow;

        private BackgroundWorker worker;

        private bool isDetecting = false;
        public bool IsDetecting
        {
            get { return isDetecting; }
            set { isDetecting = value; OnPropertyChanged("IsDetecting"); }
        }

        private long okCount = 0;
        public long OkCount
        {
            get { return okCount; }
            set { okCount = value; OnPropertyChanged("OkCount"); }
        }

        private long ngCount = 0;
        public long NgCount
        {
            get { return ngCount; }
            set { ngCount = value; OnPropertyChanged("NgCount"); }
        }

        public MainWindowViewModel(Window mainWindow,IntPtr cameraViewerHandle, HWindow imageViewerHandler)
        {
            this.mainWindow = mainWindow;
            this.cameraViewerHandle = cameraViewerHandle;
            this.imageViewerHandler = imageViewerHandler;
            this.CameraController = new CameraControllerViewModel(new CameraController(this.cameraViewerHandle));
            this.ImageProcessor = new ImageProcessorViewModel(new ImageProcessor(this.imageViewerHandler));
        }

        public CameraControllerViewModel CameraController { get; private set; }

        public ImageProcessorViewModel ImageProcessor { get; private set; }

        #region OpenImageProcSettingCommand
        private ICommand openImageProcSettingCommand;
        public ICommand OpenImageProcSettingCommand
        {
            get
            {
                if (openImageProcSettingCommand == null)
                {
                    openImageProcSettingCommand = new RelayCommand((e) => OnOpenImageProcSettingCamera(), (e) => OnCanOpenImageProcSettingCamera(e));
                }
                return openImageProcSettingCommand;
            }
        }
        private bool OnCanOpenImageProcSettingCamera(object e)
        {
            return !this.IsDetecting;
        }
        private void OnOpenImageProcSettingCamera()
        {
            ImageProcessSettingWnd setWnd = new ImageProcessSettingWnd() { Owner = this.mainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner};
            this.ImageProcessor.ResetModelImageFile();
            this.ImageProcessor.CurrentModelIC = null;
            setWnd.DataContext = this.ImageProcessor;
            setWnd.Show();
        }
        #endregion

        #region Set Command
        private ICommand openCameraSettingCommand;
        public ICommand OpenCameraSettingCommand
        {
            get
            {
                if (openCameraSettingCommand == null)
                {
                    openCameraSettingCommand = new RelayCommand((e) => OnOpenCameraSetting(), (e) => OnCanOpenCameraSetting(e));
                }
                return openCameraSettingCommand;
            }
        }
        private bool OnCanOpenCameraSetting(object e)
        {
            return !this.IsDetecting && this.CameraController.IsOpen;
        }

        private void OnOpenCameraSetting()
        {
            this.CameraController.OpenCameraSetting();
        }
        #endregion

        #region StartDetect Command
        private ICommand startDetectCommand;
        public ICommand StartDetectCommand
        {
            get
            {
                if (startDetectCommand == null)
                {
                    startDetectCommand = new RelayCommand((e) => OnStartDetect(), (e) => OnCanStartDetect(e));
                }
                return startDetectCommand;
            }
        }
        private bool OnCanStartDetect(object e)
        {
            return !this.IsDetecting && this.CameraController.IsOpen;
        }
        private void OnStartDetect()
        {
            StartDetect();
        }
        #endregion

        #region OpenCamera Command
        private ICommand openCameraCommand;
        public ICommand OpenCameraCommand
        {
            get
            {
                if (openCameraCommand == null)
                {
                    openCameraCommand = new RelayCommand((e) => OnOpenCamera(), (e) => OnCanOpenCamera(e));
                }
                return openCameraCommand;
            }
        }
        private bool OnCanOpenCamera(object e)
        {
            return  !this.CameraController.IsOpen && !this.IsDetecting;
        }
        private void OnOpenCamera()
        {
            OpenCamera();
        }
        #endregion

        #region Snapshot Command
        private ICommand snapshotCommand;
        public ICommand SnapshotCommand
        {
            get
            {
                if (snapshotCommand == null)
                {
                    snapshotCommand = new RelayCommand((e) => OnSnapshot(), (e) => OnCanSnapshot(e));
                }
                return snapshotCommand;
            }
        }
        private bool OnCanSnapshot(object e)
        {
            return this.CameraController.IsOpen && !this.IsDetecting;
        }
        private void OnSnapshot()
        {
            Snapshot();
        }

       
        #endregion

        #region CloseCamera Command
        private ICommand closeCameraCommand;
        public ICommand CloseCameraCommand
        {
            get
            {
                if (closeCameraCommand == null)
                {
                    closeCameraCommand = new RelayCommand((e) => OnCloseCamera(), (e) => OnCanCloseCamera(e));
                }
                return closeCameraCommand;
            }
        }
        private bool OnCanCloseCamera(object e)
        {
            return  this.CameraController.IsOpen && !this.IsDetecting;
        }
        private void OnCloseCamera()
        {
            CloseCamera();
        }
        #endregion

        #region StopDetect Command
        private ICommand stopDetectCommand;
        public ICommand StopDetectCommand
        {
            get
            {
                if (stopDetectCommand == null)
                {
                    stopDetectCommand = new RelayCommand((e) => OnStopDetect(), (e) => OnCanStopDetect(e));
                }
                return stopDetectCommand;
            }
        }
        private bool OnCanStopDetect(object e)
        {
            return this.IsDetecting;
        }
        private void OnStopDetect()
        {
            StopDetect();
        }
        #endregion

        #region Detect Once Command
        private ICommand detectOnceCommand;
        public ICommand DetectOnceCommand
        {
            get
            {
                if (detectOnceCommand == null)
                {
                    detectOnceCommand = new RelayCommand((e) => OnDetectOnce(), (e) => OnCanDetectOnce(e));
                }
                return detectOnceCommand;
            }
        }
        private bool OnCanDetectOnce(object e)
        {
            return !this.IsDetecting && this.CameraController.IsOpen;
        }

        private void OnDetectOnce()
        {
            if (!OpenCamera())
            {
                return;
            }

            //if (!CheckModelFile())
            //{
            //    return;
            //}

            this.IsDetecting = true;
            if (this.CameraController.Snapshot())
            {
                if (!LoadICModels())
                {
                    return;
                }

                if (this.ImageProcessor.Detect())
                {
                    this.OkCount = this.OkCount + 1;
                    //System.Windows.MessageBox.Show("合格");
                }
                else
                {
                    this.NgCount = this.NgCount + 1;
                    //System.Windows.MessageBox.Show("不合格");
                }
            }
            else
            {
                MessageBox.Show("拍摄图像失败！");
            }
            this.IsDetecting = false;
        }
        #endregion

        public bool LoadICModels()
        {
            if (!this.ImageProcessor.LoadICModels())
            {
                MessageBox.Show("加载芯片模板失败！", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        #region ClearRecords Command
        private ICommand clearRecordsCommand;
        public ICommand ClearRecordsCommand
        {
            get
            {
                if (clearRecordsCommand == null)
                {
                    clearRecordsCommand = new RelayCommand((e) => OnClearRecords(), (e) => OnCanClearRecords(e));
                }
                return clearRecordsCommand;
            }
        }
        private bool OnCanClearRecords(object e)
        {
            return true;
        }
        private void OnClearRecords()
        {
            this.OkCount = 0;
            this.NgCount = 0;
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

        private bool OpenCamera()
        {
            if (!this.CameraController.IsOpen && !this.CameraController.OpenCamera())
            {
                System.Windows.MessageBox.Show("打开相机失败！");
                return false;
            }
            return true;
        }

        private bool Snapshot()
        {
            if (!this.CameraController.IsOpen || !this.CameraController.Snapshot())
            {
                System.Windows.MessageBox.Show("拍摄照片失败！");
                return false;
            }
            System.Threading.Thread.Sleep(100);
            return this.ImageProcessor.ShowImage();
        }

        private bool CloseCamera()
        {
            if (this.CameraController.IsOpen && !this.CameraController.CloseCamera())
            {
                System.Windows.MessageBox.Show("关闭相机失败！");
                return false;
            }
            return true;
        }

        public bool CheckModelFile()
        {
            if (!this.ImageProcessor.HasValidModelFile())
            {
                System.Windows.MessageBox.Show("没有有效的模板文件");
                return false;
            }
            return true;
        }

        public bool StartDetect()
        {
            if (!OpenCamera())
            {
                return false;
            }

            if (!CheckModelFile())
            {
                return false;
            }

            if (this.worker != null && this.worker.IsBusy)
            {
                return false;
            }

            if (this.worker == null)
            {
                this.worker = new BackgroundWorker();
                this.worker.WorkerSupportsCancellation = true;
                this.worker.WorkerReportsProgress = true;
                this.worker.DoWork += new DoWorkEventHandler(OnCyclicDetect);
                this.worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnCyclicDetectCompleted);
            }
            this.IsDetecting = true;

            if (!LoadICModels())
            {
                return false;
            }

            this.worker.RunWorkerAsync();
            return true;
        }

        private void OnCyclicDetectCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.IsDetecting = false;
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnCyclicDetect(object sender, DoWorkEventArgs e)
        {
            var interval = new TimeSpan(0, 0, 1);
            var startTime = DateTime.Now;
            bool cancel = false;
            while (true)
            {
                while ((DateTime.Now - startTime).Ticks < interval.Ticks)
                {
                    if (this.worker.CancellationPending)
                    {
                        cancel = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }

                if (cancel)
                {
                    break;
                }

                startTime = DateTime.Now;
                if (this.worker.CancellationPending)
                {
                    cancel = true;
                    break;
                }

                if (!this.CameraController.Snapshot())
                {
                    break;
                }
                if (this.ImageProcessor.Detect())
                {
                    this.OkCount = this.OkCount + 1;
                }
                else
                {
                    this.NgCount = this.NgCount + 1;
                }
            }
        }

        public bool StopDetect()
        {
            if (this.worker != null && this.worker.IsBusy)
            {
                this.worker.CancelAsync();
            }
            return true;
        }
    }
}
