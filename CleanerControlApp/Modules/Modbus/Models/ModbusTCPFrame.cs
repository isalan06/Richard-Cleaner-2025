using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Modules.Modbus.Models
{
    public class ModbusTCPFrame : INotifyPropertyChanged
    {
        private byte _slaveAddress;
        public byte SlaveAddress
        {
            get => _slaveAddress;
            set => SetProperty(ref _slaveAddress, value);
        }

        private byte _functionCode;
        public byte FunctionCode
        {
            get => _functionCode;
            set
            {
                if (SetProperty(ref _functionCode, value))
                {
                    // FunctionCodeName depends on FunctionCode
                    OnPropertyChanged(nameof(FunctionCodeName));
                }
            }
        }

        public ModbusFunctionCode FunctionCodeName
        {
            get => (ModbusFunctionCode)FunctionCode;
            set => FunctionCode = (byte)value;
        }

        private ushort _startAddress;
        public ushort StartAddress
        {
            get => _startAddress;
            set => SetProperty(ref _startAddress, value);
        }

        private ushort _dataNumber;
        public ushort DataNumber
        {
            get => _dataNumber;
            set => SetProperty(ref _dataNumber, value);
        }

        private ushort[]? _data = Array.Empty<ushort>();
        public ushort[]? Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }
        private bool[]? _boolData = Array.Empty<bool>();
        public bool[]? BoolData
        {
            get => _boolData;
            set => SetProperty(ref _boolData, value);
        }

        private bool _hasResponse = false;
        public bool HasResponse
        {
            get => _hasResponse;
            set => SetProperty(ref _hasResponse, value);
        }

        private bool _hasException = false;
        public bool HasException
        {
            get => _hasException;
            set => SetProperty(ref _hasException, value);
        }

        private bool _isRead = false;
        public bool IsRead
        {
            get => _isRead;
            set => SetProperty(ref _isRead, value);
        }

        private bool _isWrite = false;
        public bool IsWrite
        {
            get => _isWrite;
            set => SetProperty(ref _isWrite, value);
        }

        private bool _emptyCommand = false;
        public bool EmptyCommand
        {
            get => _emptyCommand;
            set => SetProperty(ref _emptyCommand, value);
        }

        private bool _finalCommand = false;
        public bool FinalCommand
        {
            get => _finalCommand;
            set => SetProperty(ref _finalCommand, value);
        }

        public ModbusTCPFrame()
        {
            SlaveAddress =1;
            FunctionCode =0x03; // Read Holding Registers
            StartAddress =0;
            DataNumber =1;
        }

        public ModbusTCPFrame(byte slaveAddress, byte functionCode, ushort startAddress, ushort dataNumber, ushort[] data, bool finalCommand = false, bool emptyCommand = false)
        {
            Set(slaveAddress, functionCode, startAddress, dataNumber, data, finalCommand: finalCommand, emptyCommand: emptyCommand);
        }

        public ModbusTCPFrame(ModbusTCPFrame frame)
        {
            this.Clone(frame);
        }

        public void Set(byte slaveAddress, byte functionCode, ushort startAddress, ushort dataNumber, ushort[]? data, bool hasResponse = false, bool hasException = false, bool finalCommand = false, bool emptyCommand = false)
        {
            SlaveAddress = slaveAddress;
            FunctionCode = functionCode;
            StartAddress = startAddress;
            DataNumber = dataNumber;
            HasResponse = hasResponse;
            HasException = hasException;
            FinalCommand = finalCommand;
            EmptyCommand = emptyCommand;

            if (data != null)
            {
                var copy = new ushort[data.Length];
                Array.Copy(data, copy, data.Length);
                Data = copy;
            }
            else
            {
                Data = null;
            }

            bool _read = false;
            if ((functionCode ==0x1) || (functionCode ==0x2) || (functionCode ==0x3) || (functionCode ==0x4)) _read = true;

            IsRead = _read;
            IsWrite = !_read;

        }

        public void Set(ushort[]? data)
        {
            if (data != null)
            {
                var copy = new ushort[data.Length];
                Array.Copy(data, copy, data.Length);
                Data = copy;
            }
            else
            Data = null;
        }

        public void Clone(ModbusTCPFrame frame)
        {
            Set(frame.SlaveAddress, frame.FunctionCode, frame.StartAddress, frame.DataNumber, frame.Data, frame.HasResponse, frame.HasException, frame.FinalCommand, frame.EmptyCommand);
        }

        public ModbusTCPFrame Clone()
        {
            return new ModbusTCPFrame(this);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

    }
}
