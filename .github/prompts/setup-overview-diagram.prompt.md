---
name: setup-overview-diagram
description: Generate a drawio diagram depicting the setup of a sample including azure infrastructure elements and high level flow
agent: agent
argument-hint: "Provide the code sample Folder Path, list of entrypoint program paths to base the diagram on and any additional instructions"
---

Consider the draw io example diagrams in `.github/examples/BlogSamples.Diagrams.Examples.xml`

Generate a drawio xml diagram in a separate file depicting the setup of the ${input:sampleFolderPath} sample including azure infrastructure elements and the high level flow that is occurring in ${input:entrypointProgramPaths}.

Align the diagram with style of the `.github/examples/BlogSamples.Diagrams.Examples.xml` file.

On scale from 0 to 10 of structure details (0 no detail, 10 maximum detail) aim for 6.

Avoid depicting resource groups and subscriptions unless there are many of them.

Show essential elements only and avoid depicting backing resources for example:
- log analytics workspace backing app insights
- storage account backing azure function
- app plan backing function

Make sure diagram is properly laid out so that it is compact yet readable.

Avoid including title, subtitle and legend in the diagram.

Make sure to follow the following additional instructions:
${input:additionalInstructions}