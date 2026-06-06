# Encoder-Command Deviation
Encoder - Pulse Count = Deviation
Pulse Command = Target Pulse - Deviation

# Driver/Motor Calculate
Driver 1r = 10,000 pulse
Encoder 18-bit => 1r = 2^18=262144 pulse
PLC-Encoder=(10000/262144) x MotorEncoder
Shuttle X 1r = 40 mm => 1 pulse = 0.004 mm
Shuttel Z 1r = 10 mm => 1 pulse = 0.001 mm
Other Z 1r = 5 mm => 1 pulse = 0.0005 mm

PLC pulse/s => Driver rpm=r/min=10000pulse/min = (10000/60) pulse/s = 166.67 pulse/s => rpm = pulse/s * 0.006

## Control
Home/Jog Mode;  pulse control; Move mode
設定
H02.00 => 1 (原本:1)
H11.00 => 0 (原本:0)
H11.01 => 1 (原本):0)
H11.04 => 1 (原本:0)
H0C.09 => 1 (原本:0)
H17.00 => 28 (原本: 0)

## Jog/Home模式
H05.00(0x5000)=0
TBL

## Move模式
Write Position to H11.12(0x110C, DWORD)
Write Speed to H11.14(0x110E, WORD, RPM)
H05.00(0x5000)=2
H31.00(0x3100) Bit0 = 1 (Start)
wait InPos Signal set H31.00(0x3100) Bit0 = 0 (Done)

SM8029
SM8329

# Clamper
X72: �۰�: Clamper�Ӥ@��y�{�}��
X73: ���: Clamper�j��}

#20260513
1. MS300�ݭn�W�[�}�ҩR�O
2. �Ҧ��y�{���A�n�i�H�_�k�B�]�wTimeout
3. DryRun�s�W���ݪ��A
4. MS300-2 SetFrequencyZero�n��^����
5. �������\�઺���s���ܽT�{
6. DryRun Pick & place�y�{������Clamper�n�צ^

#20260517
1. �y�{�[ Delay
2. Dry Run �{�ǻ����b�B�z
3. Door ON�O�}...OFF�O��...
4. Semi Op �n���� �T�{�n�h����m�O�_�i��

Shuttle X => 72682 -> 72920 -> 72918 -> 72918
Shuttle Z => 143326

#20260523
Freq-4
20 => 0 kg
380 => 3 kg

#20260524
1. �s�WRecipe - Done
2. ���M���_�ʧ@�T�{����-Done
3. �����ܰʧ@�O�� - Done
4. Auto �Ŷ] - Done
5. TC-4�]�w - Done

bug
Unobserved task exception: System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (�s�Ȫ� Hwnd �O�L�Ī��C)
Unobserved task exception: System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (�s�Ȫ� Hwnd �O�L�Ī��C)
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Maximum amount of data 127 registers. (Parameter 'NumberOfPoints')
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Maximum amount of data 127 registers. (Parameter 'NumberOfPoints')
Checksums failed to match 255, 255, 255 != 255, 255, 255, 2, 3
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255

#20260525
1. �[���� �b L�� LL�S������.....�n�T�{


Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Array networkBytes must contain an even number of bytes.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 254, 255
Checksums failed to match 255, 247, 255 != 255, 247, 255, 247, 223
Checksums failed to match 255, 252, 255 != 255, 252, 255, 254, 255
Checksums failed to match 254, 254, 252 != 254, 254, 252, 255, 190
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 250, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 253, 255 != 255, 253, 255, 254, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 253
Checksums failed to match 255, 239, 239 != 255, 239, 239, 239, 239
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 254
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 253, 255, 255 != 253, 255, 255, 255, 255
Checksums failed to match 255, 255, 127 != 255, 255, 127, 254, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 127
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 191, 255, 255 != 191, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 254, 255, 255 != 254, 255, 255, 254, 255
Checksums failed to match 68, 216, 191 != 68, 216, 191, 255, 243
Checksums failed to match 254, 255, 127 != 254, 255, 127, 255, 255
Function code 95 not supported.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 190 != 255, 255, 190, 255, 191
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 254
Checksums failed to match 255, 255, 127 != 255, 255, 127, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Array networkBytes must contain an even number of bytes.
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 223
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Function code 39 not supported.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 2, 3
Array networkBytes must contain an even number of bytes.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 191 != 255, 255, 191, 255, 255
Checksums failed to match 255, 191, 255 != 255, 191, 255, 255, 255
Checksums failed to match 63, 255, 254 != 63, 255, 254, 255, 254
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 252, 255 != 255, 252, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 127, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 191, 223, 191 != 191, 223, 191, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 127 != 254, 255, 127, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Function code 63 not supported.
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 3 != 255, 255, 3, 3, 16
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 251, 251
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 254
Array networkBytes must contain an even number of bytes.
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 253, 254 != 255, 253, 254, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 254, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 191, 127 != 255, 191, 127, 255, 127
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Array networkBytes must contain an even number of bytes.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 254, 127 != 255, 254, 127, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 254 != 254, 255, 254, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 247, 255
Checksums failed to match 255, 255, 191 != 255, 255, 191, 255, 255
Checksums failed to match 247, 255, 191 != 247, 255, 191, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 253, 255 != 255, 253, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 247
Checksums failed to match 255, 255, 2 != 255, 255, 2, 3, 16
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 191 != 255, 255, 191, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 127, 255, 127 != 127, 255, 127, 127, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 254, 255
Checksums failed to match 255, 255, 1 != 255, 255, 1, 3, 16
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 247, 255, 255 != 247, 255, 255, 255, 255
Checksums failed to match 255, 191, 255 != 255, 191, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 254, 255
Checksums failed to match 249, 255, 255 != 249, 255, 255, 255, 95
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 254
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 185, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 239, 255, 127 != 239, 255, 127, 254, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 254, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 230
Function code 127 not supported.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 239, 255, 255 != 239, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 189, 253
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Array networkBytes must contain an even number of bytes.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 254 != 255, 255, 254, 254, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 251
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 254, 255
Checksums failed to match 252, 255, 255 != 252, 255, 255, 255, 254
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 255, 239 != 255, 255, 239, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 253 != 255, 255, 253, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 254, 255 != 254, 254, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 254
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 252, 255 != 255, 252, 255, 255, 255
Checksums failed to match 255, 255, 191 != 255, 255, 191, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 254, 255 != 254, 254, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Checksums failed to match 255, 254, 255 != 255, 254, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
2026-05-25 22:02:01.9229|TRACE|CleanerControlApp.Vision.LoginWindow|�}�l�i��n�J�y�{
2026-05-25 22:02:01.9335|INFO|CleanerControlApp.Vision.LoginWindow|�ϥΪ� 'admin' �n�J���\�A����GAdministrator
info: CleanerControlApp.Vision.LoginWindow[0]
      �ϥΪ� 'admin' �n�J���\�A����GAdministrator
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
2026-05-25 22:03:29.4994|TRACE|CleanerControlApp.Vision.LoginWindow|�}�l�i��n�J�y�{
2026-05-25 22:03:29.5027|INFO|CleanerControlApp.Vision.LoginWindow|�ϥΪ� 'supervisor' �n�J���\�A����GDeveloper
info: CleanerControlApp.Vision.LoginWindow[0]
      �ϥΪ� 'supervisor' �n�J���\�A����GDeveloper
Array networkBytes must contain an even number of bytes.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Array networkBytes must contain an even number of bytes.
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 254, 255, 255 != 254, 255, 255, 255, 255
Array networkBytes must contain an even number of bytes.
Maximum amount of data 127 registers. (Parameter 'NumberOfPoints')


# �ݽT�{�u�@�y�{�]�Х��T�{�A�A����U�@�B�^

�H�U����ĳ���v�B�@�k�A�нT�{���@�����u�n������]A:���q��/���R�FB:�����u�ơ^�C

1) ��l�q�ơ]��ĳ�u���^
1.1 �ڭ̷|���Ψt��/�{���h�u��q�ư��D�ӷ��GWPR/WPA �� Visual Studio Performance Profiler�A���30~60 ���� trace�C
1.2 �ؼСG��X ISR/DPC�Bdriver�B�έ��� Thread/��k�y����֤߰��ϥβv�A�H�� Modbus/USB I/O �������I�C
1.3 �Y�ݡA�ڷ|����²�檺�ާ@�B�J�ΩR�O��U�A�� trace�]�ڤ��|�b���g�A�P�N�U��������^�C

2) �إ߰�Ǵ��ա]�p�n���u�ƫe�����^
2.1 �Y�n�Ŷq�u�ƮĪG�A�|���إߤ@��²�檺 benchmark�]�ϥ� BenchmarkDotNet�^�w��̭��n���q�T�j��� I/O ���|�C
2.2 �sĶ�ýT�O��ǯ�q�L��A���� benchmark�A���o baseline�C

3) �O�u�u�ơ]���I�C�A����^
3.1 �w�����G��h�ӼҲժ� loop interval �q10ms �אּ50ms�]�A�i�[��ﵽ���p�^�C
3.2 �Y�������D�G��q�T�K���Ҳէאּ LongRunning �M�ΰ�����A�קK ThreadPool �����C
3.3 �i��G�N�ӱM�ΰ�����]�w thread affinity�]�j�֡^�A���j�ַ|���C�t�μu�ʡA�Цb�q����M�w�C

4) �n���u�ơ]�b����ǻP trace �����p�U�^
4.1 �ھ� trace�A�u�Ƴ̯Ӯɪ��{���X���|�]�X�� Modbus �ШD�B��ΫD�P�B I/O�B����W�c�u�ɥ��ȵ��^�C
4.2 �ܧ�᭫�s���� benchmark �P trace�A�P baseline ������G�C

5) ���p�P�ʱ�
5.1 �Y�ĪG�ŦX�w���A�N�ܧ�ǤJ�]�w�]�Ҧp�� loop interval �P affinity �]���i�t�m�^�C
5.2 �[�J²���E�_��x�]loop �ӮɡBmodbusŪ�g���ơB�W�ɦ��ơ^�H�Q���Ӱl�ܡC

-- �`�N�ƶ� --
- �ڷ|�b�A�T�{�u�n���q���٬O�����u�ơv��A�A�}�l����� 1 �β� 3 �B�C�Ц^�ЧA��ܪ����|�έn�ڥ��B�z���Ҳա]��ĳ���q���F�Y�n�����u�ƫ�ĳ�q `DeltaMS300` �P `TemperatureControllers` �}�l�^�C

# Profiler ����B�J�]�b�{������^

�U���O��ĳ�A�b�{�����檺�B�J�A���� Visual Studio �� Performance Profiler�]�A�����p�� WPR/WPA ���t�μh trace�^�C�����бN���ͪ��ɮצ^�ǡA�ڷ|��U���R�C

## A. Visual Studio Performance Profiler�]���μh�^
1. �}�ҧA�� solution�]Visual Studio�^�C
2. ���G`Debug` �� `Performance Profiler...`�]�Ϋ� `Alt+F2`�^�C
3. �Ŀ�G`CPU Usage (Sampling)`�A���n�ɤ]�Ŀ� `.NET Runtime` �� `Concurrency`�C
4. �I `Start` �}�l�`���C
5. �b Visual Studio Profiler ����������{���D�]��ĳ��30~60 ���^�C
6. �I `Stop` �����Ķ��C�N���G�x�s�]File �� Save As �� Export�^�A���ͪ��ɮ׳q�`�� `.diagsession` �� `.vspx`�C
7. �O�U�G������� CPU �� Thread ID�BHot Path �P�I�s���|�]Call Tree�^�C
8. �N�x�s�� profiler �ɮס]���^�����ù��I�ϡ^�^�ǵ��ڡC

## B. WPR / WPA�]�t�μh�A�ˬd DPC/ISR�B�X�ʡ^
1. �Y�h���X��/IRQ/DPC ���D�A�Цw�� Windows ADK �� Windows Performance Toolkit�]�Y�|���w�ˡ^�C
2. ���� Windows Performance Recorder (WPR)�G�Ŀ� `CPU usage (sampled)` �P `DPC/ISR`�]�]�i�[ `Hardware Interrupts`�^�C
3. Start recording�A���{���D30~60 ���A�M�� Stop�A�x�s�� `.etl` �ɡC
4. �� Windows Performance Analyzer (WPA) �}�� `.etl`�A�ˬd `CPU Usage (Sampled)`�B`DPC/ISR`�H�έ��Ӯ֤�/driver �e�γ̦h�ɶ��C
5. �N `.etl` �ɩΧA�I�Ϫ����R���צ^�ǵ��ڡC

## C. �ɥR���q�ˬd�]�ֳt�{���ާ@�^
- �b���D�o�ͮɥ��} Task Manager�]�� Process Explorer�^�A���� `Details`�A���{���� PID�A�T�{�O���Ӯ֤߭t���̰��úI�ϡC
- �p�G�� log �ɩ� Modbus/Serial �� timeout �T���A�]�@�֫O�d�C

## D. �W��/�^�ǫ�ĳ
- �ⲣ�ͪ��ɮס]`.diagsession` / `.vspx` / `.etl`�^���A��W�Ǫ���m�]Dropbox / OneDrive /�������b Ticket�^�A�����Y��ǵ��ڡC
- �Y�ɮפӤj�A���� profiler �� Summary �P�X�i���n�e���I�ϡC

---

�ڷ|�b�A�^�� trace ���U���R�õ��X�U�@�B�u�ƫ�ĳ�]�Ҧp��� LongRunning thread�B�j�֩ΦX�� Modbus �ШD�^�C