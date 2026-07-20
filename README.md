# BluetoothBatteryMonitor

실행 파일: `BluetoothBatteryMonitor.exe`

Windows가 배터리 잔량을 제공하는 Bluetooth 키보드, 마우스, 헤드폰, 게임 컨트롤러만 모아 작업표시줄과 바탕화면에 표시하는 Windows 11용 유틸리티 뼈대입니다.

## 현재 구현

- Windows PnP의 `DEVPKEY_Device_BatteryLevel` 속성을 읽는 실제 장치 공급자
- 연결 기기 대시보드와 배터리 단계별 색상 표시
- 시스템 트레이 아이콘, 좌클릭 팝오버, 우클릭 메뉴
- 대시보드에서 원하는 기기만 작업표시줄 표시 대상으로 선택
- 선택한 기기마다 키보드(K), 마우스(M), 헤드폰(H), 게임패드(G) 표식이 있는 독립 배터리 아이콘 제공
- 드래그 가능한 바탕화면 위젯과 항상 위 표시 옵션
- 시작 프로그램, 위젯, 부족 알림, 임계값 설정 저장
- 60초 자동 갱신과 수동 새로 고침
- 실제 장치 없이 UI를 확인하는 `--demo` 모드

## 실행

```powershell
dotnet run --project .\BluetoothBatteryMonitor.csproj -- --live
```

Debug 빌드는 화면 검증을 위해 기본적으로 시연 데이터를 사용합니다. 명시적인 디자인/시연 모드:

```powershell
dotnet run --project .\BluetoothBatteryMonitor.csproj -- --demo
```

## 구조

- `WindowsBatteryDeviceProvider`: Windows가 공개한 배터리 속성만 수집
- `IBatteryDeviceProvider`: 향후 WinRT Bluetooth GATT 공급자나 제조사별 어댑터를 추가하는 경계
- `TrayApplicationContext`: 앱 수명, 새로 고침, 알림, 트레이 상태 관리
- `DashboardForm`, `DesktopWidgetForm`, `TrayPopoverForm`: 서로 독립된 UI 표면

일부 제조사는 전용 리시버나 앱을 통해서만 배터리 값을 노출합니다. 이 경우 Windows 장치 속성에 값이 없으므로 현재 버전에는 표시되지 않으며, 이후 공급자 어댑터로 확장할 수 있습니다.
