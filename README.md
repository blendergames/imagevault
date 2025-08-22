# ImageVault
개인 이미지 저장소(퍼스널 포토 볼트)

## 사용자 가이드

### 개요
- 이미지 업로드, 썸네일 생성, 설명(텍스트) 기반 검색을 지원하는 개인용 이미지 보관소입니다.
- 프론트엔드: Vue 3 + Vite, 백엔드: .NET 8 Minimal API(ImageSharp로 썸네일 생성).
- 로그인: Google OAuth(프로덕션) 또는 개발 모드 Dev 로그인(로컬 테스트).

### 주요 기능
- 이미지 업로드: JPEG/PNG/WebP/GIF 업로드, 자동 썸네일 생성(128x128).
- 이미지 검색: 설명에 포함된 텍스트로 간단 검색.
- 공개 보기: 썸네일/원본 이미지를 URL로 접근 가능.
- 인증: Google 로그인 또는 개발 모드용 Dev 로그인.

### 디렉터리 구조
- `backend/`: ASP.NET Core Minimal API, 이미지 저장 및 API 제공
- `frontend/`: Vue 3 SPA, Vite 개발 서버, API 프록시(`/api` → `http://localhost:5080`)
- `GOOGLE_CREDENTIALS_GUIDE_KO.md`: Google 인증 키 발급 가이드

### 사전 준비물
- 백엔드: .NET SDK 8.x
- 프론트엔드: Node.js 18+ (권장 LTS), npm
- 선택(프로덕션): Google OAuth 클라이언트 ID/Secret

### 빠른 시작(개발 환경)
1) 백엔드 실행
   - 터미널에서 `backend/` 이동
   - `dotnet restore` 후 `dotnet run`
   - 기본 포트: `http://localhost:5080` (환경변수 `ASPNETCORE_URLS`로 변경 가능)

2) 프론트엔드 실행
   - 터미널에서 `frontend/` 이동
   - `npm install`
   - `npm run dev`
   - 접속: `http://localhost:5173` (Vite dev 서버) — `/api` 요청은 백엔드로 프록시됨

3) 초기 설정(앱 내 UI)
   - 첫 접속 시 “초기 설정: MariaDB 연결” 화면이 보입니다.
   - DB 정보 입력 후 저장하면 `backend/app_data/config.json` 파일로 저장됩니다.
   - 현재 버전은 이 설정 유무로 초기화 완료 상태를 판단하며, 실제 DB 연결은 추후 기능 확장 시 사용됩니다.

4) 로그인
   - 개발 모드: “Dev 로그인” 버튼으로 즉시 로그인 가능(프론트에서 Vite 개발 모드에서만 노출).
   - Google 로그인: `/api/auth/login` 경유. 프로덕션에서는 Google OAuth 설정 필수.

5) 사용법
   - 상단 검색창에 설명 텍스트를 입력하면 해당 텍스트가 포함된 이미지가 조회됩니다.
   - “이미지 업로드” 버튼으로 파일 선택 후 “파일 설명”을 입력하면 저장 및 인덱싱됩니다.
   - 썸네일 클릭 시 원본 이미지를 새 탭에서 열 수 있습니다.

### 환경 변수(백엔드)
- `GOOGLE_CLIENT_ID`: Google OAuth 클라이언트 ID
- `GOOGLE_CLIENT_SECRET`: Google OAuth 클라이언트 Secret
- `ASPNETCORE_URLS`: 서버 바인딩 주소(예: `http://0.0.0.0:5080`)

Google OAuth가 설정되지 않았거나 개발 환경인 경우 Dev 로그인 엔드포인트가 활성화됩니다.

자격 증명 발급은 `GOOGLE_CREDENTIALS_GUIDE_KO.md`를 참고하세요.

### 데이터 저장 위치(백엔드)
- 설정 파일: `backend/app_data/config.json`
- 이미지 저장 루트: `backend/app_data/images/`
  - 각 이미지 디렉터리: `images/{id}/`
    - 원본: `original.<ext>`
    - 썸네일: `thumb.jpg`
  - 인덱스 파일: `images/index.json` (간단 메타데이터 목록)

### API 요약
- 헬스체크: `GET /api/health` → `{ status: "ok" }`
- 설정 상태: `GET /api/config/status`
- 설정 저장: `POST /api/config` (JSON 본문: `dbHost/dbPort/dbName/dbUser/dbPassword`)
- 로그인 시작: `GET /api/auth/login` (설정 완료 및 Google OAuth 활성 시)
- 로그아웃: `POST /api/auth/logout`
- 현재 사용자: `GET /api/auth/me` (인증 필요)
- Dev 로그인: `GET /api/auth/dev-login` (개발/무OAuth 시)
- Dev 로그아웃: `POST /api/auth/dev-logout`
- 이미지 업로드: `POST /api/images` (FormData: `file`, `description`, 인증 필요)
- 이미지 검색: `GET /api/images/search?q=...`
- 썸네일 보기: `GET /api/images/{id}/thumb`
- 원본 보기: `GET /api/images/{id}/original`

### 배포 참고(요약)
- 백엔드: `dotnet publish -c Release`로 빌드 후 실행 또는 컨테이너화.
- 프론트엔드: `npm run build`로 `frontend/dist/` 산출물 생성 → 정적 호스팅(예: Nginx) 또는 백엔드에 정적 파일 서빙 추가 구성.
- 리버스 프록시(Nginx 등)에서 `/api`는 백엔드로, 정적 파일은 프론트 배포물로 라우팅.
- 환경 변수로 Google OAuth와 바인딩 주소를 설정.

### 트러블슈팅
- 401 Unauthorized: 로그인 필요. 개발 모드면 Dev 로그인 사용 가능.
- 503 Service Unavailable(로그인 시): 초기 설정 미완료 또는 Google OAuth 미구성.
- 업로드 실패: 파일 형식(JPG/PNG/WebP/GIF)과 크기 확인. 서버 콘솔 로그 확인.
- OAuth 리다이렉트 오류: 콘솔에 등록된 Redirect URI와 백엔드 콜백(`/api/auth/callback/google`) 정확히 일치 확인.

### 로드맵(예시)
- DB 연동을 통한 메타데이터/권한 관리 강화
- 태그/앨범, 공유 링크, 만료 정책
- 정적 파일 통합 서빙(백엔드 단일 호스트)

문의/피드백은 이슈로 남겨주세요. 🙌
