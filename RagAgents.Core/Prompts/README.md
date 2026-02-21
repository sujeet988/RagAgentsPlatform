# Prompt Management Guide

## Overview

This directory contains the prompt templates used by RagAgents. Prompts are externalized to allow:
- ✅ Changes without redeployment
- ✅ Version control and rollback
- ✅ A/B testing different variants
- ✅ Environment-specific prompts

## File Structure

```
Prompts/
├── prompts.yml              # Main prompt configuration (runtime)
├── prompts.production.yml   # Production overrides (optional)
├── Library/                 # Prompt documentation & history
│   ├── rag-simple-v1.2.md
│   └── ...
└── README.md               # This file
```

## Prompt Format (YAML)

```yaml
rag_prompts:
  prompt_name:
    version: "1.0"
    description: "Human-readable description"
    parameters:
      - param1
      - param2
    template: |
      Your multi-line prompt here.
      Use {param1} and {param2} as placeholders.
```

## Using Prompts in Code

```csharp
// Inject IPromptProvider
private readonly IPromptProvider _promptProvider;

// Get a prompt
var prompt = _promptProvider.GetSimpleRagPrompt(context, question);

// Or for custom prompts
var prompt = _promptProvider.GetCustomPrompt("summarize", new Dictionary<string, string>
{
    ["text"] = documentText,
    ["max_words"] = "100"
});
```

## Versioning Strategy

### Version Format
- `major.minor` (e.g., `1.2`)
- **Major**: Breaking changes (parameter changes, complete rewrites)
- **Minor**: Improvements, tweaks, bug fixes

### When to Increment Version
- **Minor (1.1 → 1.2)**: Improved wording, added instructions
- **Major (1.x → 2.0)**: Changed parameters, different approach

### Version History
Track changes in the `Library/` markdown files for detailed history.

## A/B Testing

To test a new prompt variant:

1. Add to `experiments` section in `prompts.yml`:
```yaml
experiments:
  my_experiment:
    enabled: true          # Set to true to activate
    base_prompt: "simple"
    version: "1.3-experimental"
    template: |
      Your experimental prompt here
```

2. Update code to conditionally use experiment
3. Monitor metrics (accuracy, latency, user feedback)
4. Promote to main or discard

## Environment-Specific Prompts

Create environment-specific files:
- `prompts.yml` - Base configuration
- `prompts.development.yml` - Dev overrides
- `prompts.production.yml` - Production overrides

The provider will merge configurations, with environment-specific taking precedence.

## Best Practices

### ✅ Do's
- Keep prompts focused and clear
- Use descriptive parameter names
- Document why changes were made
- Test prompts with edge cases
- Version all changes
- Include examples in Library/ markdown files

### ❌ Don'ts
- Don't embed secrets or PII in prompts
- Don't make prompts too long (>2000 tokens)
- Don't use ambiguous instructions
- Don't deploy without testing
- Don't forget to update version numbers

## Prompt Engineering Tips

1. **Be Specific**: Clear instructions yield better results
2. **Use Examples**: Include few-shot examples when needed
3. **Set Constraints**: "Use only context", "Be concise", etc.
4. **Handle Edge Cases**: What if context is empty? Question unclear?
5. **Iterate**: Test → Measure → Improve → Repeat

## Monitoring

Track these metrics for each prompt:
- Average response time
- Token usage (input + output)
- User satisfaction (thumbs up/down)
- Error rate
- Accuracy (if ground truth available)

## Rollback Procedure

If a prompt version causes issues:

1. **Immediate**: Revert `prompts.yml` to previous commit
```bash
git checkout HEAD~1 -- Prompts/prompts.yml
```

2. **Restart**: Reload configuration (or restart service if needed)

3. **Investigate**: Check logs, metrics, user feedback

4. **Fix**: Update prompt in a new branch, test thoroughly

## Contributing

When adding/modifying prompts:

1. Create a feature branch
2. Update `prompts.yml` with new/modified prompt
3. Increment version number
4. Add detailed markdown doc in `Library/`
5. Test with representative queries
6. Create PR with before/after examples
7. Get review from team + prompt engineer

## Examples

See `Library/` directory for detailed examples with:
- Full prompt text
- Expected behavior
- Test cases
- Version history
