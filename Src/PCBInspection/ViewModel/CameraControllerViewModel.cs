using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using PCBInspection.Model;

namespace PCBInspection.ViewModel
{
    class CameraControllerViewModel : INotifyPropertyChanged
    {
        public CameraController CameraController { get; private set; }

        public CameraControllerViewModel(CameraController controller)
        {
            this.CameraController = controller;
        }

        public bool IsOpen
        {
            get
            {
                return this.CameraController != null && this.CameraController.IsOpen;
            }
        }

        #region Open Command
        private ICommand openCommand;
        public ICommand OpenCommand
        {
            get
            {
                if (openCommand == null)
                {
                    openCommand = new RelayCommand((e) => OnOpenCamera(), (e) => OnCanOpenCamera(e));
                }
                return openCommand;
            }
        }
        private bool OnCanOpenCamera(object e)
        {
            return !this.IsOpen;
        }
        private void OnOpenCamera()
        {
            OpenCamera();
        }
        #endregion

        #region Close Command
        private ICommand closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                if (closeCommand == null)
                {
                    closeCommand = new RelayCommand((e) => OnCloseCamera(), (e) => OnCanCloseCamera(e));
                }
                return closeCommand;
            }
        }
        private bool OnCanCloseCamera(object e)
        {
            return this.IsOpen;
        }
        private void OnCloseCamera()
        {
            CloseCamera();
        }
        #endregion

        #region Set Command
        private ICommand setCommand;
        public ICommand SetCommand
        {
            get
            {
                if (setCommand == null)
                {
                    setCommand = new RelayCommand((e) => OnSetCamera(), (e) => OnCanSetCamera(e));
                }
                return setCommand;
            }
        }
        private bool OnCanSetCamera(object e)
        {
            return this.IsOpen;
        }

        private void OnSetCamera()
        {
            if (this.IsOpen)
            {
                this.CameraController.OpenSetting();
            }
        }
        #endregion

        #region Preview Command
        private ICommand previewCommand;
        public ICommand PreviewCommand
        {
            get
            {
                if (previewCommand == null)
                {
                    previewCommand = new RelayCommand((e) => OnPreviewCamera(e), (e) => OnCanPreviewCamera(e));
                }
                return previewCommand;
            }
        }
        private bool OnCanPreviewCamera(object e)
        {
            return this.IsOpen;
        }

        private void OnPreviewCamera(object param)
        {
            if (this.IsOpen)
            {
                bool isPreview;
                if (bool.TryParse(param.ToString(), out isPreview))
                {
                    if (isPreview)
                    {
                        this.CameraController.StartVedio();
                    }
                    else
                    {
                        this.CameraController.StopVedio();
                    }
                }
            }
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
                    snapshotCommand = new RelayCommand((e) => OnSnapshotCamera(), (e) => OnCanSnapshotCamera(e));
                }
                return snapshotCommand;
            }
        }
        private bool OnCanSnapshotCamera(object e)
        {
            return this.IsOpen;
        }
        private void OnSnapshotCamera()
        {
            Snapshot();
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

        public bool OpenCamera()
        {
            if (!this.IsOpen)
            {
                if (this.CameraController != null)
                {
                    if (this.CameraController.Init())
                    {
                        int count = 0;
                        if (this.CameraController.SearchCameras(out count) && count != 0)
                        {
                            if (this.CameraController.OpenCamera(0))
                            {
                                this.CameraStatus = "已连接";
                                return this.CameraController.StartVedio();
                            }
                        }
                    }
                }
                return false;
            }
            return true;
        }

        public bool Snapshot()
        {
            if (this.IsOpen)
            {
                return this.CameraController.Snapshot();
            }
            return false;
        }

        public bool CloseCamera()
        {
            if (this.IsOpen)
            {
                if (this.CameraController.CloseCamera())
                {
                    this.CameraStatus = "未连接";
                }
            }
            return true;
        }

        private string cameraStatus = "未连接";
        public string CameraStatus
        {
            get { return cameraStatus; }
            set { cameraStatus = value; OnPropertyChanged("CameraStatus"); }
        }
    }
}
