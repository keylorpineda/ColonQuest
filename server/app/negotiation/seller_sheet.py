"""The seller's hidden sheet: the truth about an installment offer, computed by the engine.

The model never invents these numbers. It only decides how and when the seller reveals each
fact, and replies that quote any other amount are rejected.
"""

from dataclasses import dataclass
from decimal import ROUND_CEILING, Decimal
from functools import cached_property

from app.schemas import SellerFact

ROUND_TO = Decimal(100)  # sellers quote installments rounded up to the next 100 colones


def format_colones(amount: int) -> str:
    """Costa Rican style: ₡31 500."""
    return "₡" + f"{amount:,}".replace(",", " ")


@dataclass(frozen=True)
class Offer:
    offer_id: str
    item: str
    seller_name: str
    cash_price: int
    term_months: int
    monthly_rate_pct: Decimal
    insurance_monthly: int
    commission: int

    @cached_property
    def monthly_payment(self) -> int:
        """French amortization of the cash price, rounded up to the next 100 colones."""
        rate = self.monthly_rate_pct / 100
        principal = Decimal(self.cash_price)
        if rate == 0:
            raw = principal / self.term_months
        else:
            raw = principal * rate / (1 - (1 + rate) ** -self.term_months)
        return int((raw / ROUND_TO).to_integral_value(rounding=ROUND_CEILING) * ROUND_TO)

    @cached_property
    def total_cost(self) -> int:
        return (self.monthly_payment + self.insurance_monthly) * self.term_months + self.commission

    @cached_property
    def facts(self) -> dict[SellerFact, str]:
        """Each fact as the seller would say it, in Costa Rican Spanish."""
        return {
            SellerFact.CASH_PRICE: format_colones(self.cash_price),
            SellerFact.MONTHLY_PAYMENT: format_colones(self.monthly_payment),
            SellerFact.TERM_MONTHS: f"{self.term_months} meses",
            SellerFact.INTEREST_RATE: f"{self.monthly_rate_pct}% mensual",
            SellerFact.INSURANCE_FEE: f"{format_colones(self.insurance_monthly)} al mes",
            SellerFact.COMMISSION: format_colones(self.commission),
            SellerFact.TOTAL_COST: format_colones(self.total_cost),
        }

    @cached_property
    def allowed_amounts(self) -> frozenset[int]:
        """Every colón amount the seller may quote."""
        return frozenset(
            {
                self.cash_price,
                self.monthly_payment,
                self.insurance_monthly,
                self.commission,
                self.total_cost,
                self.monthly_payment + self.insurance_monthly,
                (self.monthly_payment + self.insurance_monthly) * self.term_months,
                self.monthly_payment * self.term_months,
            }
        )


OFFERS: dict[str, Offer] = {
    offer.offer_id: offer
    for offer in (
        Offer(
            offer_id="celular_gama_alta",
            item="celular de gama alta",
            seller_name="Kevin",
            cash_price=350_000,
            term_months=24,
            monthly_rate_pct=Decimal("2.5"),
            insurance_monthly=3_500,
            commission=15_000,
        ),
        Offer(
            offer_id="pantalla_65",
            item="pantalla de 65 pulgadas",
            seller_name="Kevin",
            cash_price=650_000,
            term_months=36,
            monthly_rate_pct=Decimal("2.9"),
            insurance_monthly=4_500,
            commission=25_000,
        ),
        Offer(
            offer_id="moto_125",
            item="moto 125 cc",
            seller_name="Doña Marta",
            cash_price=1_200_000,
            term_months=48,
            monthly_rate_pct=Decimal("2.2"),
            insurance_monthly=9_000,
            commission=40_000,
        ),
        Offer(
            offer_id="refri_doble_puerta",
            item="refrigeradora de doble puerta",
            seller_name="Doña Marta",
            cash_price=450_000,
            term_months=30,
            monthly_rate_pct=Decimal("2.7"),
            insurance_monthly=0,
            commission=20_000,
        ),
    )
}


def get_offer(offer_id: str) -> Offer | None:
    return OFFERS.get(offer_id)
