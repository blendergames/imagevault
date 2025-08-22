# ImageVault용 Google OAuth 로그인 설정 가이드

이 프로젝트는 ASP.NET Core(.NET 8)의 Google OAuth(서버 사이드 Authorization Code) 방식으로 로그인을 처리합니다. 브라우저는 백엔드의 `/api/auth/login`으로 이동해 Google 로그인으로 리다이렉트되고, Google이 인증 후 백엔드 콜백(`/api/auth/callback/google`)으로 돌아옵니다. 이후 쿠키 인증으로 세션이 유지됩니다.

## 핵심 요약
- 인증 방식: 서버 사이드 OAuth 2.0 Authorization Code (Microsoft.AspNetCore.Authentication.Google)
- 리디렉션 경로: `/api/auth/callback/google` (고정)
- 로그인 시작 엔드포인트: `GET /api/auth/login`
- 환경 변수: `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`
- 개발/무설정 시: Dev 로그인 엔드포인트 활성화(테스트용)

## 1) Google Cloud에서 OAuth 클라이언트 만들기
1. Google Cloud Console → 상단 프로젝트 선택(또는 새 프로젝트 생성)
2. API 및 서비스 → OAuth 동의화면 → 사용자 유형 선택(개발 시 External + 테스트 사용자 추가 권장) → 앱 정보 입력 → 저장
3. API 및 서비스 → 사용자 인증 정보(Credentials) → 사용자 인증 정보 만들기 → OAuth 클라이언트 ID → 앱 유형 "웹 애플리케이션"
4. 승인된 리디렉션 URI에 다음을 추가
   - 로컬 개발: `http://localhost:5080/api/auth/callback/google`
   - (선택) LAN/프록시 사용 시: `http://<서버IP또는호스트>:5080/api/auth/callback/google`
   - 프로덕션: `https://<your-domain>/api/auth/callback/google`
   - 주의: 값은 정확히 일치해야 합니다(경로, 프로토콜, 포트 포함)
5. 생성 후 표시되는 "클라이언트 ID"와 "클라이언트 보안 비밀(Client secret)"을 복사합니다.

참고: 이 프로젝트의 플로우는 서버 리다이렉트 기반이므로 "승인된 JavaScript 원본"은 필요하지 않습니다(Implicit/Pkce SPA가 아님).

## 2) 백엔드에 자격 정보 설정
백엔드 프로세스가 다음 두 값을 읽습니다(환경변수 또는 `appsettings.json`).

- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`

또는 `appsettings.json`에 다음과 같이 설정할 수 있습니다.

```
{
  "Google": {
    "ClientId": "<복사한 Client ID>",
    "ClientSecret": "<복사한 Client Secret>"
  }
}
```

추가로 바인딩 주소를 바꾸려면 `ASPNETCORE_URLS`를 사용하세요(기본: `http://0.0.0.0:5080`).

## 3) 동작 확인 절차
1. 백엔드: `backend/`에서 `dotnet run` 실행 → `http://localhost:5080/api/health`가 `{ status: "ok" }` 반환 확인
2. 프론트엔드: `frontend/`에서 `npm run dev` → `http://localhost:5173` 접속
3. 초기 설정 화면에서 DB 정보를 저장(설정 파일은 `backend/app_data/config.json`에 생성되며, 설정 완료 전에는 로그인 차단)
4. 네비게이션/로그인 화면에서 "Google로 로그인" 클릭 → Google 로그인/동의 → 앱으로 리다이렉트
5. 우상단 사용자 정보 노출 또는 `GET /api/auth/me`가 사용자 JSON을 반환하면 성공

## 자주 발생하는 오류와 해결
- `redirect_uri_mismatch`
  - 콘솔에 등록한 리디렉션 URI가 정확히 `http(s)://호스트[:포트]/api/auth/callback/google`와 일치하는지 확인
  - 프록시 사용 시 외부에서 보이는 최종 도메인/프로토콜 기준으로 등록
- `invalid_client` / `invalid_client_secret`
  - 올바른 프로젝트의 최신 클라이언트 정보를 사용했는지 확인
  - 환경변수/설정 키 오타 확인(`GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`)
- 로그인 시 503 반환
  - `config.json` 미생성 또는 필수 필드 미입력 시 로그인 차단됨 → 앱 초기 설정 완료 필요
  - Google OAuth 미구성 시에도 로그인 차단됨(개발 모드/무설정 시엔 Dev 로그인 사용 가능)
- 로컬에서만 성공, 프로덕션에서 실패
  - 프로덕션 도메인용 리디렉션 URI 추가 누락 가능성
  - 프록시가 경로를 재작성하지 않는지 확인(`/api/auth/callback/google` 그대로 전달)

## 보안 팁
- Client Secret은 서버 측 비밀로만 보관하고, 저장소에 커밋하지 않습니다.
- 이 플로우는 서버 쿠키 세션을 사용합니다. 프런트엔드에 토큰을 노출하지 않습니다.
- 필요한 범위는 기본 프로필/이메일이며(프로바이더 기본), 민감 범위를 추가할 경우 동의화면 검수가 필요할 수 있습니다.

## 개발 편의(Dev 로그인)
- 개발 환경이거나 Google OAuth가 미구성인 경우, 다음 엔드포인트로 테스트 로그인이 가능합니다.
  - `GET /api/auth/dev-login` → 로그인
  - `POST /api/auth/dev-logout` → 로그아웃
- 프로덕션 환경에서는 Google OAuth 구성을 완료하고 Dev 로그인을 사용하지 않는 것을 권장합니다.

