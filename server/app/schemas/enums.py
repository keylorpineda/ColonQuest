"""Closed enums shared by the backend, the model prompts and the Unity client."""

from enum import StrEnum


class EventType(StrEnum):
    INCOME = "income"
    EXPENSE = "expense"
    EMERGENCY = "emergency"
    OFFER = "offer"
    SCAM = "scam"
    MACRO_NEWS = "macro_news"
    BONUS = "bonus"


class EffectKind(StrEnum):
    EXPENSE = "expense"
    INCOME = "income"
    NEW_INSTALLMENT = "new_installment"
    NEW_INFORMAL_LOAN = "new_informal_loan"
    SAVINGS_CHANGE = "savings_change"
    WELLBEING_CHANGE = "wellbeing_change"
    PRICE_INFLATION = "price_inflation"


class MoneySource(StrEnum):
    CASH = "cash"
    SAVINGS = "savings"
    EMERGENCY_FUND = "emergency_fund"
    CREDIT_CARD = "credit_card"


class TeachingConcept(StrEnum):
    BUDGET = "budget"
    EMERGENCY_FUND = "emergency_fund"
    COMPOUND_INTEREST = "compound_interest"
    MINIMUM_PAYMENT = "minimum_payment"
    INSTALLMENTS_TOTAL_COST = "installments_total_cost"
    INFORMAL_LENDING = "informal_lending"
    PYRAMID_SCHEME = "pyramid_scheme"
    INFLATION = "inflation"
    EXCHANGE_RATE = "exchange_rate"
    PAYROLL_DEDUCTIONS = "payroll_deductions"


class DealStatus(StrEnum):
    OPEN = "open"
    ACCEPTED = "accepted"
    REJECTED = "rejected"
    WALKED_AWAY = "walked_away"


class SellerFact(StrEnum):
    """Facts on the hidden seller sheet that the NPC can reveal."""

    CASH_PRICE = "cash_price"
    MONTHLY_PAYMENT = "monthly_payment"
    TERM_MONTHS = "term_months"
    INTEREST_RATE = "interest_rate"
    INSURANCE_FEE = "insurance_fee"
    COMMISSION = "commission"
    TOTAL_COST = "total_cost"


class KeyQuestion(StrEnum):
    """Questions a financially literate player should ask (impact metric)."""

    TOTAL_COST = "total_cost"
    HIDDEN_FEES = "hidden_fees"
    INTEREST_RATE = "interest_rate"
    TERM = "term"
    CASH_PRICE = "cash_price"
    INSURANCE = "insurance"
