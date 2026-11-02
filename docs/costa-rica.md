<p align="center"><b>English</b> | <a href="costa-rica.es.md">Español</a></p>

<p align="center">
  <img src="assets/costa-rica-en.svg" width="100%" alt="The situation of debt and financial education in Costa Rica, with cited figures. The full content of this card is available as text in the section below.">
</p>

<details open>
<summary><b>Read this page as text</b></summary>

## The situation in Costa Rica

Why a personal-finance simulator set in Costa Rica, with the figures behind each decision of the game.
Every number below links to where it was published. Most come through the national press quoting the
original institution, and we say so. Consulted on 2 November 2026.

### Debt is ordinary

The Oficina del Consumidor Financiero (OCF) surveyed 1 200 people aged 18 to 70 by phone in July 2025,
representing about 3.6 million adults, with a margin of error of 2.8%. [^ocf]

| Figure | What it measures |
| --- | --- |
| **87%** | Have some kind of debt (91% in 2023). It counts any debt, so the stronger figure is the next one |
| **34%** | Of monthly income goes to paying debts, on average (38% in 2023, 52% in 2020) |
| **58.3%** | The same share for people who earn under ₡500 000 a month |
| **14.7%** | The same share at ages 18 to 24, far below the average |
| **About 1 in 3** | Households have months when money arrives late and they have to borrow |

Our reading of the 14.7%: young people have not filled up with debt yet, and that is the moment to
practice. It is why the player of ColonQuest is 18 and starts with a first job.

### Nobody teaches it

| Figure | What it measures |
| --- | --- |
| **80.88%** | Of the people surveyed never received any financial education |
| **73.89%** | Say at least one course would be useful to them |

Survey by CINPE and IDESPO at the Universidad Nacional, 1 705 people aged 18 or older, reported in
April 2024. [^cinpe] Law 10627, to promote financial education, has been in force since 18 February
2025 and was regulated on 11 February 2026, [^law] so the country is now asking for exactly this.

### The traps of the game are real

**Gota a gota.** Lenders who ask for little more than an ID, and collect daily.

| Figure | What it measures | Source |
| --- | --- | --- |
| **2 930** | Cases linked to gota a gota in 2025, nationwide | OIJ, press conference [^oij] |
| **2 050** | Cases so far in 2026, as of August | OIJ, press conference [^oij] |
| **20% to 100%** | Interest found in these loans | OIJ [^oij] |
| **₡5 000 to ₡15 000** | Paid per day by some borrowers | OIJ [^oij] |
| **3%, about 92 000 people** | Took a gota a gota loan in the last three years (12% in 2023) | OCF [^informal] |
| **50 000 to 66 000** | People who hold one right now | OCF [^informal] |
| **14%** | Received loan offers by WhatsApp, Facebook or TikTok in the last year | OCF [^informal] |

The sources do not agree, and we do not hide it. Another count of the OIJ gives 1 662 complaints for
2025 and 2026 together, with 653 in 2026, [^teletica] against the 2 930 and 2 050 cases above,
probably because they count different things. And the survey shows gota a gota falling while police
cases rise. We use the OIJ's figure from its own press conference, as a case count, not as a number
of victims.

**The card and the minimum payment.** The Banco Central sets a legal maximum every six months: **36.48%
a year** in colones for the second half of 2026, and cards fall under it. Microcredit has its own cap of
51.51%. [^usury] 18% of people say they finance purchases with a card. [^ocf]

**Installments.** 22% mention debts with appliance stores, cooperatives and solidarity associations.
[^ocf] It is the debt the sellers of the game offer: one small payment, and insurance you did not ask
about.

**Pyramids.** In 2021 Sugef sent at least 50 complaints about apparent pyramid scams to the prosecutors.
[^sugef] In August 2025 the prosecutors were investigating a scheme across Costa Rica, Panama and Mexico,
where those affected believe there are more than 600 investors. [^fiscalia]

### The engine uses the real rules

The AI tells the story; these numbers are what the engine computes with.

| In the game | The real rule | Source |
| --- | --- | --- |
| Gross salary ₡373 092 | Minimum wage for an unskilled worker in 2026, ₡373 092.30 a month | Decree 45303-MTSS, [^wage] and `GameRules.cs` |
| 10.83% off the salary | Worker's charges in 2026: SEM 5.50, IVM 4.33, Banco Popular 1.00 | Checked in [the review of the engine](ANALISIS_RUBRICA_README_MOTOR.md) |
| Income tax exempt up to ₡918 000 | 2026 brackets: 10%, 15%, 20% and 25% above that | Decree 45333-H, same review |
| Aguinaldo of one twelfth | Exempt from charges and income tax | Law 2412, same review |
| Card at 3% a month | 36% a year, under the cap of 36.48% | [^usury] |
| Lender at 20% a month | The low end of what the OIJ found | [^oij] |

Simplifications we chose: income tax has no credits for children or a spouse, and the prices of the
town are examples, as the game itself says.

### What the game does about it

| What happens in Costa Rica | What the player practices |
| --- | --- |
| A third of income goes to debts | Living one month at a time on the real salary, with the real close of the month |
| Installments with costs nobody mentions | Asking the seller for the total, the insurance and the commission, out loud |
| Paying only the minimum | Seeing the balance grow at the real rate |
| A lender a message away | Asking what it costs, and meeting the collectors if you sign |
| A friend with a sure business | Asking where the money comes from before putting yours in |

**What we cannot claim yet:** that it works. We have no pilot results to publish, so this page describes
the problem and the design, not an effect. Class Mode and the telemetry per decision are there to measure
it, and the results will go in `docs/pilot/`.

### Sources

[^ocf]: OCF, Encuesta Nacional de Endeudamiento 2025, reported by [Delfino.cr, 22 October 2025](https://delfino.cr/2025/10/aunque-los-costarricenses-comprometen-en-promedio-menos-dinero-para-el-pago-de-deudas-el-nivel-de-endeudamiento-es-alto-para-buena-parte-de-la-poblacion).
[^informal]: OCF, same survey, reported by [Delfino.cr, 4 November 2025](https://delfino.cr/2025/11/ocf-advierte-que-el-credito-informal-sigue-afectando-a-los-sectores-con-menos-ingresos-en-costa-rica).
[^cinpe]: CINPE and IDESPO (UNA), reported by [El Financiero, 8 April 2024](https://www.elfinancierocr.com/economia-y-politica/una-cartera-muy-estrecha-menos-del-10-de-las/22FABMYC6ZE47JR73C2UYYMBJU/story).
[^law]: Dates of Law 10627, from an opinion column in [La Nación, 20 May 2026](https://www.nacion.com/opinion/foros/entre-la-tarjeta-y-la-loteria-tenemos-cultura/LRYCPI4W65HN3IG2EQ256KSCPU/story/).
[^oij]: OIJ, press conference, reported by [Semanario Universidad, 11 August 2026](https://semanariouniversidad.com/pais/oij-registra-mas-de-2-000-casos-vinculados-a-prestamos-gota-a-gota-en-lo-que-va-de-2026/).
[^teletica]: OIJ, reported by [Teletica, 21 August 2026](https://www.teletica.com/nacional/prestamos-gota-a-gota-acumulan-mas-de-1600-denuncias-en-menos-de-dos-anos_415829).
[^usury]: BCCR, reported by [Delfino.cr, July 2026](https://www.delfino.cr/2026/07/topes-de-usura-tendran-ajustes-mixtos-para-el-segundo-semestre).
[^sugef]: [Teletica, 29 June 2021](https://www.teletica.com/288694_sugef-traslada-a-fiscalia-50-denuncias-por-aparentes-estafas-en-piramides).
[^fiscalia]: [CRHoy, 26 August 2025](https://crhoy.com/fiscalia-investiga-multimillonario-fraude-con-esquema-piramidal-en-tres-paises/).
[^wage]: Decree 45303-MTSS, with the 1.63% raise from 1 January 2026, reported by [KPMG Costa Rica](https://kpmg.com/cr/es/insights/2025/11/newsflash-nov-7-2025.html) and [Bloomberg Línea](https://www.bloomberglinea.com/latinoamerica/costa-rica/salario-minimo-2026-en-costa-rica-nuevos-montos-para-el-sector-privado/).

</details>
