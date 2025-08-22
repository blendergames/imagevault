# backend

.NET (ASP.NET Core) 기반 백엔드 구현 디렉터리입니다.

제안하는 다음 단계:
- .NET SDK 버전 결정(예: .NET 8 LTS)
- `dotnet new webapi`로 기본 스캐폴딩 생성
- Nullable/Analyzers/Style 설정, `appsettings` 분리
- 간단한 Health 체크 엔드포인트 추가
- 테스트 프로젝트(xUnit/NUnit) 구성

원하시면 여기서 바로 `dotnet new webapi` 스캐폴딩까지 진행하겠습니다.

## appsettings.json 생성 가이드

Google OAuth를 사용하려면 백엔드 구성에서 `appsettings.json` 파일을 생성해 값을 넣을 수 있습니다(또는 환경 변수 사용).

- 파일 경로: `backend/appsettings.json`
- 파일 형식(JSON):

```json
{
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  }
}
```

참고사항
- 개발 환경에서는 `backend/appsettings.Development.json`도 인식됩니다.
- 민감정보이므로 저장소 커밋은 지양하세요. 대안으로 환경 변수 `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`를 사용할 수 있습니다.
- 애플리케이션은 `builder.Configuration["Google:ClientId"]`, `builder.Configuration["Google:ClientSecret"]`에서 값을 읽습니다.
