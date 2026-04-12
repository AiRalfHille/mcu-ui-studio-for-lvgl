# Preface

MCU UI Studio for LVGL did not begin as part of a large product plan. It
started with a much simpler question:

I wanted to better understand what an LVGL screen actually looks like as
native C code.

Other editors I looked at certainly have their own strengths, but for my own
way of learning they did not provide the direct view into generated LVGL code
that I was looking for.

My first goal was much smaller:

- understand LVGL better
- see the generated code
- understand how a screen becomes concrete C structures and LVGL calls

That wish led to the idea of building an editor and generator specifically for
this workflow.

At the same time, I had been interested for a while in how far AI-supported
development could be taken. I wanted to find out whether a tool could be built
in a form that I probably would not have created alone.

This project therefore emerged over many iterations together with Codex and
Claude, without a large roadmap, but with a clear first goal: a minimal tool
that actually works.

The conceptual direction, the architecture, and the overall structure of the
application were defined by me. AI was used as an implementation tool for
individual building blocks, while the technical decisions, system structure,
and integration of the components remained under my control.

Step by step, this small beginning grew into more:

- a structured editor
- a native LVGL preview path
- C code generation for the display side
- thinking around contracts between UI and MCU logic
- and, in the longer term, stronger bindings

Not all of this is finished yet. Many parts are still evolving, and some
exist only in an early form. But that is part of the project itself: it was
not designed as a finished blueprint, but grew out of actual work.

And perhaps that is the most important point:

Building this tool has not only been useful for me, but also an enjoyable and
genuinely interesting process.

This manual documents the current state of that journey.
