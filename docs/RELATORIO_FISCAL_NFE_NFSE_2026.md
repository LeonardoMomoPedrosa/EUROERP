# Relatório fiscal — Emissão de NF-e e NFS-e (EURO ERP)

Data da análise: 16/11/2026

Escopo: verificar se a emissão de NF-e (modelo 55) e NFS-e está adequada às regras atuais de IBS, CBS, ICMS, PIS/COFINS e ISS.

## Resumo

A parte da Reforma Tributária (IBS/CBS) está em grande parte adequada para 2026. O que precisa de atenção é o ICMS, o PIS/COFINS, algumas tags da NF-e e três itens da NFS-e.

Nenhum código foi alterado: a maioria das correções depende do regime tributário da empresa e precisa ser confirmada pelo contador.

Limitações:
- Não foi possível compilar nem validar um XML contra o XSD, porque o ambiente da análise não tinha .NET instalado e o projeto não tem testes.
- A análise é do código e da configuração de produção (`appsettings.Production.json`).
- As regras de 2026 citadas (NT 2025.002, NT 004 da NFS-e nacional e NT 2026.010 do DANFE) devem ser conferidas com a versão vigente de cada uma.

## NF-e: IBS/CBS (adequado para 2026)

- **Alíquotas de teste**: CBS 0,9%, IBS estadual 0,1% e IBS municipal 0%, como prevê a LC 214/2025 para 2026. Elas ficam configuráveis na tabela de controle do sistema (`SYS_CONTROL`: `IBS_ALIQ_UF`, `IBS_ALIQ_MUN`, `CBS_ALIQ`).
- **Grupos do XML**: o item recebe `IBSCBS` com CST 000, `cClassTrib` 000001 e os blocos `gIBSUF`, `gIBSMun` e `gCBS`. O total recebe `IBSCBSTot` e `vNFTot`, e cada item recebe `vItem`. Em 2026 o IBS/CBS não soma no total da nota, e o código respeita isso.
- **Base de cálculo**: é o valor do item menos o desconto, deduzidos ICMS, PIS, COFINS, FCP e ISS. Segue a fórmula da NT (`NfeXmlBuilder.CalcIbsCbs`).
- **XSD**: os schemas em `src/EUROERP.Web/Schemas` já incluem os tipos da Reforma (`DFeTiposBasicos_v1.00.xsd`, `IBSCBS`, `vItem`, `vNFTot`).

**Ressalvas no IBS/CBS:**
- O CST e o `cClassTrib` são únicos para a empresa toda. Itens com redução, isenção ou monofasia sairiam classificados como tributação integral. O plano em `docs/DANFE_NT_2026.010_PLAN.md` já prevê parâmetros por produto.
- O grupo IBS/CBS sai também quando a empresa é do Simples Nacional (CRT 1). Para o Simples, o IBS/CBS só é exigido a partir de 2027. Produção usa CRT 3, então hoje isso não afeta.

## NF-e: problemas encontrados (por gravidade)

### 1. DANFE sem IBS/CBS, com prazo em 01/12/2026

Pelo plano do projeto (`docs/DANFE_NT_2026.010_PLAN.md`), a NT 2026.010 exige IBS, CBS e IS no DANFE impresso a partir de 01/12/2026. O `NfePdfGenerator.cs` ainda não tem nada disso, e o plano registra que algumas colunas de ICMS dos itens estão fixas em `0,00`.

### 2. PIS e COFINS com alíquotas de regimes diferentes

Produção usa PIS 1,65% e COFINS 3%. São combinações de regimes distintos:
- Lucro Real (não cumulativo): PIS 1,65% e COFINS 7,6%.
- Lucro Presumido (cumulativo): PIS 0,65% e COFINS 3%.

Uma das duas está errada. Como PIS e COFINS são deduzidos da base do IBS/CBS, essa base também sai errada. O CST 01 está fixo nos dois.

### 3. ICMS sempre CST 00 a 18%, qualquer que seja a operação

Com CRT 3 o código ignora o cadastro fiscal do produto (o mapeamento `CSTB_ID` para CSOSN só é usado no Simples):
- **Venda para outro estado**: aplica 18% em vez da alíquota interestadual de 4%, 7% ou 12%.
- **Venda para outro estado a consumidor não contribuinte**: o grupo do diferencial de alíquota (`ICMSUFDest`) não é gerado, e a SEFAZ rejeita a nota (rejeição 694).
- **Substituição tributária**: autopeças e peças de ar-condicionado frequentemente têm ST em SP, e a nota sairia como tributação normal.
- **Origem fixa em "0" (nacional)**: produtos importados exigem origem 1 ou 2 e alíquota de 4% nas vendas interestaduais.

### 4. Indicadores fixos no grupo de identificação da nota (`ide`)

- `indFinal=1` (consumidor final) inclusive em vendas para revenda entre empresas.
- `indPres=1` (presencial) inclusive em pedidos do Mercado Livre. Para venda pela internet, o correto é 2 ou 9, e nesse caso o grupo do intermediador (`indIntermed` / `infIntermed`) também é obrigatório.
- Se nenhum CFOP for encontrado, o código usa 5102, que é de operação interna. Numa venda para outro estado isso gera rejeição.

### 5. Itens menores

- O pagamento sai como "01 – Dinheiro, à vista", mas a nota leva duplicata com vencimento em 15 dias.
- O valor aproximado de tributos (`vTotTrib`, Lei 12.741/2012) não é informado.
- Em `appsettings.Development.json`, o código de município do emitente é 3550308 (São Paulo), quando deveria ser 3547304 (Santana de Parnaíba). Produção está correta.

## NFS-e (Simpliss, padrão nacional, Santana de Parnaíba)

O que está adequado: o DPS na versão 1.01 (`NfesDpsBuilder.cs`) já envia o grupo `IBSCBS` com `finNFSe`, `indFinal`, `cIndOp`, `indDest`, CST e `cClassTrib`. No padrão nacional quem calcula os valores de IBS/CBS é o próprio ambiente nacional, então não enviar valores está correto.

Problemas:
1. **Código NBS vazio em produção.** O `cNBS` só é enviado quando configurado, e a configuração de produção (`Nfes:CodigoNbs`) está vazia. Pela NT 004 ele passou a ser obrigatório para IBS/CBS, então há risco de rejeição. É preciso preencher o NBS do serviço do código 14.01.
2. **Indicador de consumidor final fixo.** O `indFinal` vem da configuração e está sempre 0, inclusive quando o tomador é pessoa física.
3. **Indicador de operação.** O `cIndOp` padrão 100301 deve ser confirmado com o contador para o serviço prestado.
4. **Flag sem efeito.** A opção "incluir IBS/CBS" (`IncludeIbsCbs` / `NFES_IBSCBS`) é salva, mas o código não a lê: o grupo sempre é enviado. Para empresa fora do Simples isso é o correto, mas a opção na tela engana.
5. **ISS.** A retenção sai sempre como "não retido" (`tpRetISSQN=1`) e a alíquota é fixa em 5%.
6. **Backend da Prefeitura de São Paulo.** `PrefeituraSpNfesBackend.cs` usa o layout antigo v01 (`PedidoEnvioLoteRPS_v01.xsd`), sem nenhum campo de IBS/CBS. Não é usado em produção hoje, mas não deve ser ativado sem atualização.

## Prioridades

1. Atualizar o DANFE antes de 01/12/2026.
2. Confirmar o regime (Lucro Real ou Presumido) e corrigir as alíquotas de PIS e COFINS.
3. Preencher o NBS na configuração da NFS-e.
4. Rever a definição de ICMS (CST, ST, alíquota interestadual, diferencial de alíquota) e dos indicadores, usando o cadastro fiscal do produto e do cliente em vez de valores fixos.
5. Passar CST e `cClassTrib` do IBS/CBS para o nível do produto, como prevê o plano.

## Arquivos analisados

- `src/EUROERP.Infrastructure/NFe/NfeXmlBuilder.cs`
- `src/EUROERP.Infrastructure/NFe/NfeIndividualService.cs`
- `src/EUROERP.Infrastructure/NFe/NfePdfGenerator.cs`
- `src/EUROERP.Infrastructure/Nfes/NfesDpsBuilder.cs`
- `src/EUROERP.Infrastructure/Nfes/NfesConfigSnapshot.cs`
- `src/EUROERP.Infrastructure/Nfes/PrefeituraSpNfesBackend.cs`
- `src/EUROERP.Web/Schemas/*.xsd`
- `src/EUROERP.Web/appsettings.Production.json` e `appsettings.Development.json`
