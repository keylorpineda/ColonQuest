import json

from app.export_schemas import build_documents, main


def test_documents_are_valid_deterministic_json() -> None:
    first = build_documents()
    assert set(first) == {
        "PlayerState.schema.json",
        "GameEvent.schema.json",
        "NegotiationReply.schema.json",
        "CreateSessionRequest.schema.json",
        "DecisionRequest.schema.json",
        "HealthResponse.schema.json",
        "NegotiationRequest.schema.json",
        "openapi.json",
    }
    assert first == build_documents()
    for content in first.values():
        json.loads(content)


def test_committed_schemas_match_models() -> None:
    assert main(["--check"]) == 0
