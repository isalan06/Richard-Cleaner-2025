# 馬達
匯川 Driver 1r = 10,000 pulse
匯川 Encoder 18-bit => 1r = 2^18=262144 pulse
換算 PLC當前脈波座標(Encoder)=(10000/262144)xEncoder
Shuttle X 1r = 40 mm => 1 pulse = 0.004 mm
Shuttel Z 1r = 10 mm => 1 pulse = 0.001 mm
Other Z 1r = 5 mm => 1 pulse = 0.0005 mm

速度換算 寫入 pulse/s => 匯川 速度是用 rpm=r/min=10000pulse/min = (10000/60) pulse/s = 166.67 pulse/s => rpm = pulse/s * 0.006

## Control 變更
Home/Jog保持用 pulse控制; Move用通訊控制
參數設定
H02.00 => 1 (原先:)
H11.04 => 1 (原先:)
H0C.09 => 1 (原先:)
H17.00 => 28 (原先: )

## Jog/Home步驟
H05.00(0x5000)=0
TBL

## Move步驟
Write Position to H11.12(0x110C, DWORD)
Write Speed to H11.14(0x110E, WORD, RPM)
H05.00(0x5000)=2
H31.00(0x3100) Bit0 = 1 (Start)
wait InPos Signal set H31.00(0x3100) Bit0 = 0 (Done)

# Clamper
X72: 自動: Clamper照一般流程開關
X73: 手動: Clamper強制打開

#20260513
1. MS300需要增加開啟命令
2. 所有流程狀態要可以復歸且設定Timeout
3. DryRun新增等待狀態
4. MS300-2 SetFrequencyZero要改回條件
5. 有長按功能的按鈕提示確認
6. DryRun Pick & place流程先拿掉Clamper要修回

#20260517
1. 流程加 Delay
2. Dry Run 程序說明在處理
3. Door ON是開...OFF是關...
4. Semi Op 要提示 確認要去的位置是否可行

Shuttle X => 72682 -> 72920 -> 72918 -> 72918
Shuttle Z => 143326

#20260523
Freq-4
20 => 0 kg
380 => 3 kg

#20260524
1. 新增Recipe - Done
2. 風刀往復動作確認次數-Done
3. 手動顯示動作燈號 - Done
4. Auto 空跑 - Done
5. TC-4設定 - Done

bug
Unobserved task exception: System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (零值的 Hwnd 是無效的。)
Unobserved task exception: System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (零值的 Hwnd 是無效的。)
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Maximum amount of data 127 registers. (Parameter 'NumberOfPoints')
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Maximum amount of data 127 registers. (Parameter 'NumberOfPoints')
Checksums failed to match 255, 255, 255 != 255, 255, 255, 2, 3
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255

#20260525
1. 加熱槽 在 L跟 LL沒有關掉.....要確認


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
2026-05-25 22:02:01.9229|TRACE|CleanerControlApp.Vision.LoginWindow|開始進行登入流程
2026-05-25 22:02:01.9335|INFO|CleanerControlApp.Vision.LoginWindow|使用者 'admin' 登入成功，角色：Administrator
info: CleanerControlApp.Vision.LoginWindow[0]
      使用者 'admin' 登入成功，角色：Administrator
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
Checksums failed to match 255, 255, 255 != 255, 255, 255, 255, 255
2026-05-25 22:03:29.4994|TRACE|CleanerControlApp.Vision.LoginWindow|開始進行登入流程
2026-05-25 22:03:29.5027|INFO|CleanerControlApp.Vision.LoginWindow|使用者 'supervisor' 登入成功，角色：Developer
info: CleanerControlApp.Vision.LoginWindow[0]
      使用者 'supervisor' 登入成功，角色：Developer
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


# 待確認工作流程（請先確認，再執行下一步）

以下為建議的逐步作法，請確認哪一條路線要先執行（A:先量測/分析；B:直接優化）。

1) 初始量化（建議優先）
1.1 我們會先用系統/程式層工具量化問題來源：WPR/WPA 或 Visual Studio Performance Profiler，抓取30~60 秒的 trace。
1.2 目標：找出 ISR/DPC、driver、或哪個 Thread/方法造成單核心高使用率，以及 Modbus/USB I/O 的延遲點。
1.3 若需，我會提供簡單的操作步驟或命令協助你抓 trace（我不會在未經你同意下直接執行）。

2) 建立基準測試（如要做優化前必做）
2.1 若要衡量優化效果，會先建立一個簡單的 benchmark（使用 BenchmarkDotNet）針對最重要的通訊迴圈或 I/O 路徑。
2.2 編譯並確保基準能通過後再執行 benchmark，取得 baseline。

3) 保守優化（風險低，先行）
3.1 已完成：把多個模組的 loop interval 從10ms 改為50ms（你可觀察改善情況）。
3.2 若仍有問題：把通訊密集模組改為 LongRunning 專用執行緒，避免 ThreadPool 排擠。
3.3 可選：將該專用執行緒設定 thread affinity（綁核），但綁核會降低系統彈性，請在量測後決定。

4) 積極優化（在有基準與 trace 的情況下）
4.1 根據 trace，優化最耗時的程式碼路徑（合併 Modbus 請求、改用非同步 I/O、減少頻繁短時任務等）。
4.2 變更後重新執行 benchmark 與 trace，與 baseline 比較結果。

5) 部署與監控
5.1 若效果符合預期，將變更納入設定（例如把 loop interval 與 affinity 設為可配置）。
5.2 加入簡易診斷日誌（loop 耗時、modbus讀寫次數、超時次數）以利未來追蹤。

-- 注意事項 --
- 我會在你確認「要先量測還是直接優化」後，再開始執行第 1 或第 3 步。請回覆你選擇的路徑及要我先處理的模組（建議先量測；若要直接優化建議從 `DeltaMS300` 與 `TemperatureControllers` 開始）。

# Profiler 抓取步驟（在現場執行）

下面是建議你在現場執行的步驟，先用 Visual Studio 的 Performance Profiler（再視情況用 WPR/WPA 做系統層 trace）。抓取後請將產生的檔案回傳，我會協助分析。

## A. Visual Studio Performance Profiler（應用層）
1. 開啟你的 solution（Visual Studio）。
2. 選單：`Debug` → `Performance Profiler...`（或按 `Alt+F2`）。
3. 勾選：`CPU Usage (Sampling)`，必要時也勾選 `.NET Runtime` 或 `Concurrency`。
4. 點 `Start` 開始蒐集。
5. 在 Visual Studio Profiler 執行期間重現問題（建議錄30~60 秒）。
6. 點 `Stop` 結束採集。將結果儲存（File → Save As 或 Export），產生的檔案通常為 `.diagsession` 或 `.vspx`。
7. 記下：明顯佔用 CPU 的 Thread ID、Hot Path 與呼叫堆疊（Call Tree）。
8. 將儲存的 profiler 檔案（或擷取的螢幕截圖）回傳給我。

## B. WPR / WPA（系統層，檢查 DPC/ISR、驅動）
1. 若懷疑驅動/IRQ/DPC 問題，請安裝 Windows ADK 的 Windows Performance Toolkit（若尚未安裝）。
2. 執行 Windows Performance Recorder (WPR)：勾選 `CPU usage (sampled)` 與 `DPC/ISR`（也可加 `Hardware Interrupts`）。
3. Start recording，重現問題30~60 秒，然後 Stop，儲存為 `.etl` 檔。
4. 用 Windows Performance Analyzer (WPA) 開啟 `.etl`，檢查 `CPU Usage (Sampled)`、`DPC/ISR`以及哪個核心/driver 占用最多時間。
5. 將 `.etl` 檔或你截圖的分析結論回傳給我。

## C. 補充輕量檢查（快速現場操作）
- 在問題發生時打開 Task Manager（或 Process Explorer），切到 `Details`，找到程式的 PID，確認是哪個核心負載最高並截圖。
- 如果有 log 檔或 Modbus/Serial 的 timeout 訊息，也一併保留。

## D. 上傳/回傳建議
- 把產生的檔案（`.diagsession` / `.vspx` / `.etl`）放到你能上傳的位置（Dropbox / OneDrive /直接附在 Ticket），或壓縮後傳給我。
- 若檔案太大，先傳 profiler 的 Summary 與幾張重要畫面截圖。

---

我會在你回傳 trace 後協助分析並給出下一步優化建議（例如改用 LongRunning thread、綁核或合併 Modbus 請求）。