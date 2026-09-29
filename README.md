# SIGEI — Sistema Integrado de Gestão Escolar

SaaS multi-escola para o ensino angolano (Iniciação à 13.ª classe), em C# / .NET 9 (Blazor + EF Core).

## Regras já implementadas (`src/Sigei.Domain`)
- Níveis: Primário (Iniciação–6.ª), I Ciclo (7.ª–9.ª), II Ciclo (10.ª–13.ª).
- Notas por trimestre: MAC, NPP, NPT → MT = (MAC+NPP+NPT)/3; média final = média dos 3 MT (positiva ≥ 10).
- Acesso às notas (somente leitura): até à 6.ª classe só o encarregado; da 7.ª em diante o aluno tem login próprio.
- A secretaria decide por matrícula: Automático (encarregado só vê com a mensalidade em dia), Liberado ou Bloqueado.
  Cada escola pode desligar a regra da mensalidade.

## Demonstração local
Em desenvolvimento cria-se automaticamente uma escola fictícia com 5 contas (direção, secretaria, professor, aluno da 8.ª, encarregado da 5.ª),
palavra-passe `Sigei@Demo2026`. Ecrãs: secretaria `/secretaria/acesso-notas`, professor `/professor/notas`, aluno/encarregado `/portal/notas`, direção `/direcao/ano-letivo` (abrir/fechar com backup), `/direcao/disciplinas`, `/direcao/definicoes`, matrículas `/secretaria/matriculas`, turmas `/secretaria/turmas`.
Backups do fecho do ano: `src/Sigei.Web/App_Data/backups` (ignorado pelo Git; contêm dados pessoais).

## Correr
```
dotnet test
dotnet run --project src/Sigei.Web
```
