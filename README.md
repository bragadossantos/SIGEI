# SIGEI — Sistema Integrado de Gestão Escolar

SaaS multi-escola para o ensino angolano (Iniciação à 13.ª classe), em C# / .NET 9 (Blazor + EF Core).

## Regras já implementadas (`src/Sigei.Domain`)
- Níveis: Primário (Iniciação–6.ª), I Ciclo (7.ª–9.ª), II Ciclo (10.ª–13.ª).
- Notas por trimestre: MAC, NPP, NPT → MT = (MAC+NPP+NPT)/3; média final = média dos 3 MT (positiva ≥ 10).
- Acesso às notas (somente leitura): até à 6.ª classe só o encarregado; da 7.ª em diante o aluno tem login próprio.
- A secretaria decide por matrícula: Automático (encarregado só vê com a mensalidade em dia), Liberado ou Bloqueado.
  Cada escola pode desligar a regra da mensalidade.

## Correr
```
dotnet test
dotnet run --project src/Sigei.Web
```
