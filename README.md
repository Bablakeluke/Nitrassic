# Nitrassic
Nitrassic is a JavaScript engine for .NET. It exchanges some ECMAScript 5 compliance for speed - this engine is currently less compliant than the original Jurassic engine it is a derivative of and instead executes code considerably faster.

# The Source
It's written in C# and requires the C# 3.5 compiler or higher. It can target the .NET 2.0 runtime. It uses no native or unsafe marked code so it is suitable for use in e.g Unity3D.

# Current Status
Nitrassic is currently under heavy development so please do not use it in a release; Use Jurassic instead. At the moment, it is only expected to run small pieces of code whilst the type tracker is still being constructed.

# Why isn't this a fork?
It's essentially a merger of two engines and is a completely different beast from either. We also wanted the initial development to happen in relative privacy so we could safely make widespread changes. Officially though, this engine is a fork of Jurassic with its internals being replaced with concepts from Nitro.

# Why the name 'Nitrassic'?
The name is a combination of 'Nitro' and 'Jurassic'. Nitro was the name of our previous internal JavaScript engine which was fast but not very compliant. Nitrassic intends to combine Jurassic's compliance with the speed of Nitro. We also wanted to drop the name Nitro as Apple also called their JavaScript engine 'Nitro' a few months after we named ours and it's caused confusion ever since.
