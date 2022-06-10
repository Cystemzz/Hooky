using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hooky
{
    public class Hook
    {
        public bool Enabled;
        private readonly MethodInfo _from;
        private readonly MethodInfo _to;
        private readonly bool _is64Bit = IntPtr.Size == 8;
        private byte[] _originalMethod;

        public Hook(MethodInfo from, MethodInfo to)
        {
            _from = from;
            _to = to;
        }

        public void Enable()
        {
            if (Enabled) throw new NotSupportedException("Method already hooked.");
            Enabled = true;
            RuntimeHelpers.PrepareMethod(_from.MethodHandle);
            RuntimeHelpers.PrepareMethod(_to.MethodHandle);

            var fromPtr = _from.MethodHandle.GetFunctionPointer();
            var toPtr = _to.MethodHandle.GetFunctionPointer();

            if (_is64Bit)
            {
                _originalMethod = Memory.ReadBytes(fromPtr, 13);
                Memory.WriteByte(fromPtr, 0x49);
                Memory.WriteByte(fromPtr + 1, 0xbb);
                Memory.WriteLong(fromPtr + 2, toPtr.ToInt64());
                Memory.WriteByte(fromPtr + 10, 0x41);
                Memory.WriteByte(fromPtr + 11, 0xff);
                Memory.WriteByte(fromPtr + 12, 0xe3);
                return;
            }
            _originalMethod = Memory.ReadBytes(fromPtr, 6);
            Memory.WriteByte(fromPtr, 0xe9);
            Memory.WriteInt(fromPtr + 1, toPtr.ToInt32() - fromPtr.ToInt32() - 5);
            Memory.WriteByte(fromPtr + 5, 0xc3);
        }

        public void Disable()
        {
            if (!Enabled) throw new NotSupportedException("Method not hooked.");
            Enabled = false;
            Memory.WriteBytes(_from.MethodHandle.GetFunctionPointer(), _originalMethod);
        }

        public T Call<T>(object instance, object[] parameters)
        {
            var isHooked = Enabled;
            if (isHooked) Disable();
            var temp = _from.Invoke(instance, parameters);
            if (isHooked) Enable();
            return (T) temp;
        }

        public void Call(object instance, object[] parameters)
        {
            var isHooked = Enabled;
            if (isHooked) Disable();
            _from.Invoke(instance, parameters);
            if (isHooked) Enable();
        }
        
        ~Hook()
        {
            if (Enabled) Memory.WriteBytes(_from.MethodHandle.GetFunctionPointer(), _originalMethod);
        }
    }
}