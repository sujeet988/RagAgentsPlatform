# Simple RAG Prompt

**Prompt ID:** `rag_prompts.simple`  
**Version:** 1.2  
**Created:** 2026-01-15  
**Last Updated:** 2026-02-21  
**Status:** Active (Production)

---

## Description

Basic retrieval-augmented generation prompt without conversation history. Used for single-turn question answering based on retrieved document context.

## Use Cases

- ✅ Single question answering
- ✅ Document search and retrieval
- ✅ FAQ systems
- ❌ Multi-turn conversations (use `with_history` instead)

---

## Template

```
Answer using ONLY the context below.
If information is missing, say "Information not available".

Context:
{context}

Question:
{question}
```

---

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `context` | string | Yes | Retrieved document chunks (concatenated) |
| `question` | string | Yes | User's question |

---

## Examples

### Example 1: Successful Answer

**Input:**
```
Context: The Eiffel Tower is located in Paris, France. It was built in 1889 and stands 330 meters tall.
Question: How tall is the Eiffel Tower?
```

**Expected Output:**
```
The Eiffel Tower stands 330 meters tall.
```

### Example 2: Missing Information

**Input:**
```
Context: The Eiffel Tower is located in Paris, France. It was built in 1889.
Question: What is the weight of the Eiffel Tower?
```

**Expected Output:**
```
Information not available.
```

### Example 3: Partial Information

**Input:**
```
Context: The Eiffel Tower attracts millions of visitors annually. It was designed by Gustave Eiffel.
Question: When was the Eiffel Tower built?
```

**Expected Output:**
```
Information not available.
```

---

## Evaluation Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Accuracy (ground truth) | >85% | 87% |
| Hallucination rate | <5% | 3% |
| Avg response time | <2s | 1.8s |
| Avg tokens (input) | ~500 | 480 |
| Avg tokens (output) | ~50 | 45 |

---

## Version History

### v1.2 (2026-02-21) - Current
- **Changed:** Added explicit "Information not available" instruction
- **Reason:** Reduced hallucination rate from 8% to 3%
- **Impact:** Model less likely to generate information not in context
- **Tested with:** 500 test queries

### v1.1 (2026-02-01)
- **Changed:** Simplified from "Answer the question below using only..." to "Answer using ONLY..."
- **Reason:** More direct instruction, fewer tokens
- **Impact:** No significant change in accuracy, 5% faster
- **Tested with:** 200 test queries

### v1.0 (2026-01-15)
- **Initial version**
- **Template:**
  ```
  Answer the question below using only the context provided.
  Do not use any external knowledge.
  
  Context:
  {context}
  
  Question:
  {question}
  ```
- **Issues:** Slightly verbose, occasional hallucinations

---

## Known Issues

1. **Empty Context:** If `{context}` is empty, model may still attempt to answer. Consider validation before calling LLM.
2. **Very Long Context:** If context exceeds model's context window (~8K tokens for GPT-3.5), truncation occurs. Consider chunking strategy.
3. **Ambiguous Questions:** Model may struggle with vague questions. Consider query expansion or clarification prompts.

---

## A/B Test Results

### Test: Simple vs. Concise (Jan 2026)
- **Hypothesis:** More concise wording improves response time
- **Result:** v1.1 (concise) performed 5% faster with same accuracy
- **Decision:** Promoted v1.1 to production

---

## Related Prompts

- **`rag_prompts.with_history`** - For multi-turn conversations
- **`rag_prompts.with_citations`** - For answers requiring source attribution

---

## Maintenance

**Owner:** AI Team  
**Reviewer:** Product Team  
**Next Review:** 2026-03-21 (monthly cadence)

---

## References

- [Original PR #123](https://github.com/your-org/ragagents/pull/123)
- [A/B Test Results Doc](https://docs.internal/ab-test-jan-2026)
- [Prompt Engineering Best Practices](https://docs.internal/prompt-engineering)
