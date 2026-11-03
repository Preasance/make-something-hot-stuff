# Kiroshi Desktop Zoom

VRChat PC 데스크톱(모니터·마우스)용 아바타 광학 스코프 프로토타입입니다. 별도 카메라의 확대 영상을 청록색 테두리와 조준선이 있는 사각 화면에 표시합니다. 플레이어 카메라의 FOV를 변경하지 않습니다. Cyberpunk 공식 에셋은 포함하지 않습니다.

## 준비

- VRChat Creator Companion이 현재 지원하는 Unity 버전으로 만든 **Avatars** 프로젝트
- VRChat SDK Avatars, 휴머노이드 아바타, Windows 빌드 타깃
- 작업 전 Unity 프로젝트와 아바타 백업

이 저장소는 완성된 Unity 프로젝트가 아닙니다. `Assets/KiroshiZoom` 폴더를 기존 프로젝트의 `Assets` 안에 복사하세요. 커스텀 런타임 스크립트나 Udon을 아바타에 넣지 않고, 에디터에서 Camera·RenderTexture·Animator·메뉴 에셋을 생성합니다.

## 생성과 연결

1. Unity 컴파일 완료 후 `Tools > Kiroshi Zoom > Create desktop zoom`을 엽니다.
2. 씬의 VRCAvatarDescriptor를 Avatar에 넣고 생성 버튼을 누릅니다.
3. `Assets/KiroshiZoom/Generated-…`에 에셋이 생성되고 Head 아래 `KiroshiZoom` 오브젝트가 생깁니다. 기본 상태는 꺼짐입니다. 생성 후 아바타의 계층과 이름을 변경하면 애니메이션 경로를 다시 만들어야 합니다.
4. 기존 FX가 없는 아바타는 Descriptor의 Playable Layers에서 FX에 생성된 `ZoomFX.controller`를 지정합니다. **기존 FX가 있으면 덮어쓰지 마세요.** 기존 컨트롤러를 복제한 뒤, 사용하는 Animator 병합 도구로 생성된 두 레이어와 세 파라미터를 병합하세요. 첫 레이어(Off/On) 가중치도 1이어야 합니다. 다른 레이어의 Write Defaults 정책과도 맞춰야 합니다.
5. 기존 Expression Parameters에 `Kiroshi/Enabled`(Bool, 기본 false, Saved 끔, Synced 끔), `Kiroshi/Amount`(Float, 기본 0, Saved 켬, Synced 끔)를 추가합니다. `IsLocal`은 VRChat 내장 파라미터이므로 Expression Parameters에 추가하지 않습니다. 기존 파라미터가 전혀 없을 때만 생성된 `Parameters.asset`을 직접 연결합니다.
6. 기존 Expressions Menu에 Sub Menu 항목을 추가해 생성된 `Menu.asset`을 연결합니다. 메뉴가 없는 아바타는 해당 에셋을 직접 지정합니다. Descriptor의 Custom Expressions 설정도 확인하세요.
7. 씬을 저장하고 SDK의 **Build & Test**로 아래 검증을 진행하세요.

## 동작과 조정

Expressions 메뉴의 `Optics`로 켜고 `Zoom` 원형 컨트롤로 조절합니다. 카메라의 수직 FOV는 30°에서 6°로 변합니다. 실제 배율은 플레이어 FOV와 화면 크기에 따라 달라집니다. 생성 카메라는 1024×1024 텍스처를 사용하며, UI(5)·UI를 위한 내부 레이어(12)·MirrorReflection(18)을 제외합니다. 해당 레이어의 월드 오브젝트는 확대 화면에 나오지 않을 수 있습니다.

이 구현은 **머리 뼈를 따라가는 스코프**입니다. 아바타만으로 플레이어 카메라를 직접 참조할 수 없어 마우스 시점과 완전히 일치한다고 보장할 수 없습니다. 아바타의 Head 회전, 로컬 스케일, View Position, VRChat의 1인칭 머리 숨김 처리에 따라 위치를 조정해야 합니다. 생성 시의 로컬 아바타 전방을 기준으로 하므로 Head 축이 다른 리그도 게임 내 확인이 필요합니다. Display의 로컬 위치·크기와 ZoomCamera 방향을 조정해 맞추세요. 전체 화면 고정 HUD, 마우스 휠 입력, VR 양안 대응, Quest 지원은 포함하지 않습니다.

`IsLocal` 조건으로 다른 사용자 인스턴스에서 카메라와 화면을 끄며, 셰이더는 VRChat 거울·사진 카메라에서 화면을 숨깁니다. 이것은 비밀 정보 접근 방지 장치가 아닙니다. 월드나 다른 아바타의 셰이더가 별도 카메라에 다르게 표시될 수도 있습니다. 카메라 렌더링은 성능 비용과 아바타 성능 등급에 영향을 줄 수 있습니다.

## Unity / VRChat 검증 체크리스트

이 클라우드 환경에는 Unity 및 VRChat 클라이언트가 없어 **컴파일·업로드·게임 내 동작은 아직 검증되지 않았습니다.**

- Unity Console 컴파일 오류가 없고 SDK 빌드 검증을 통과하는지 확인합니다.
- Build & Test에서 기본 상태가 꺼져 있고 Optics를 켰을 때 실제 월드가 확대되어 보이는지 확인합니다. 검은 화면, 좌우 반전, 화면 반복이 없는지도 확인합니다.
- Zoom 양 끝에서 원거리 표지판의 크기가 변하고, 껐다 켜도 배율이 유지되는지 확인합니다.
- 위·아래·좌·우로 시점을 움직여 화면 정렬과 머리 숨김 처리를 확인합니다. 정렬이 맞지 않으면 이 아바타에서 헤드 기반 방식이 적합한지 먼저 판단하세요.
- 두 번째 클라이언트에서 화면과 카메라가 꺼져 있는지, 거울·사진 카메라에 HUD가 보이지 않는지 확인합니다.
- 기존 표정·의상 FX와 메뉴가 정상인지, 줌을 끌 때 프레임 속도가 회복되는지 확인합니다.

제거하려면 Head 아래 생성된 오브젝트와 추가한 FX 레이어·파라미터·메뉴 항목을 제거합니다. 생성 에셋은 참조를 제거한 후 삭제하세요.
