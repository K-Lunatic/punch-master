# Punch Master

Unity 기반 오락실 펀치머신 게임 프로젝트입니다.

M5Atom/MPU6886 IMU 센서의 가속도·자이로 데이터를 ESP32-S3 수신 모듈과 시리얼 통신으로 Unity에 전달하고, 펀치 강도와 속도를 점수로 변환합니다.

## 주요 구현

- Unity 6 + C#
- M5Atom / MPU6886 IMU
- ESP32-S3 serial integration
- IMU CSV parsing and COM port auto-detection
- Punch detection using acceleration/gyro magnitude and threshold filtering
- 0.3-second peak measurement window
- Power 80% / Speed 20% score normalization
- Gauge, score animation, and audio feedback
- Automatic reconnect and keyboard fallback mode

## 실행

1. Unity 6000.3.9f1로 프로젝트를 엽니다.
2. `Assets/Scenes/play.unity`를 엽니다.
3. IMU 수신 장치를 연결합니다.
4. 하드웨어가 없으면 Space 키로 테스트할 수 있습니다.

하드웨어 펌웨어와 Unity 프로젝트는 별도 구성입니다.
