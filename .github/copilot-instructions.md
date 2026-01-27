---
description: "Guidelines for using GitHub Copilot effectively in this repository."
applyTo: **
name: "GitHub Copilot Instructions"
---
This project represents a control library for rendering graph controls in WPF and Avalonia applications. 

When writing code, include clear and descriptive comments.

Avoid using terminal command tools unless absolutely nessecary

Performance is critical. This library should function efficiently even with large (1000+ nodes) graphs. Allocations should be minimized during rendering and interaction. Make use of performance tools like `stackalloc` and `Span<T>` where appropriate to reduce heap allocations.

Rendering logic should be optimized for speed. Use efficient algorithms and data structures to ensure smooth rendering and interaction with the graph controls.

Point out any potential odd logic choices or anti-patterns that could lead to performance bottlenecks or unexpected behavior.

When thinking about performance, keep in mind the WPF / Avalonia rendering pipelines, and avoid unnessacry measures / layouts / renders.

Ensure that any code generated follows best practices for WPF and Avalonia development, including proper use of data binding, MVVM patterns, and resource management.

