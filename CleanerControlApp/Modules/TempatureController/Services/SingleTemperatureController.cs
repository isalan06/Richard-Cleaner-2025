using CleanerControlApp.Modules.TempatureController.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.TempatureController.Services
{
    public class SingleTemperatureController : ISingleTemperatureController, IDisposable
    {
        #region Constants

        public static readonly int BUFFER_SIZE = 8;

        #endregion

        #region attribute

        private ushort[]? _buffers = null;

        #endregion

        #region constructor

        // Removed IServiceProvider parameter - no service-locator usage
        public SingleTemperatureController()
        {
            _buffers = new ushort[BUFFER_SIZE];
        }

        #endregion

        #region desturctor & IDisposable

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~SingleTemperatureController()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region ISingleTempatureController

        public int SV
        { 
            get => _buffers != null && _buffers.Length > 0 ? (int)(short)_buffers[0] : 0;
            set
            {
                if (_buffers != null && _buffers.Length > 0)
                {
                    _buffers[0] = (ushort)value;
                }
            }
        }
        public int PV => _buffers != null && _buffers.Length > 1 ? (int)(short)_buffers[1] : 0;
        public int Un => _buffers != null && _buffers.Length > 2 ? (int)(short)_buffers[2] : 0;
        public float Ctu => _buffers != null && _buffers.Length > 3 ? (float)(short)_buffers[3] : 0;
        public ushort Status => _buffers != null && _buffers.Length > 4 ? _buffers[4] : (ushort)0;
        public int AL1 => _buffers != null && _buffers.Length > 5 ? (int)(short)_buffers[5] : 0;
        public int AL2 => _buffers != null && _buffers.Length > 6 ? (int)(short)_buffers[6] : 0;
        public float HB => _buffers != null && _buffers.Length > 7 ? (float)(short)_buffers[7] : 0;

        public void SetData(ushort[]? data)
        {
            if (data == null || data.Length < BUFFER_SIZE)
            {
                // handle error or ignore
                return;
            }
            if (_buffers == null || _buffers.Length != BUFFER_SIZE)
            {
                _buffers = new ushort[BUFFER_SIZE];
            }
            Array.Copy(data, _buffers, BUFFER_SIZE);
        }

        #endregion
    }
}
