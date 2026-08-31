# Needle In A Haystack Simulator

Unity 6 WebGL game: the player sorts thousands of hay straws and finds one needle.

Architecture is decided. Do not invent a per-straw GameObject/physics design.

1. Read `.cursor/rules/haystack-constitution.mdc` (always on in Cursor).
2. Before hay / pickup / pile / instancing / WebGL rendering work, read `Docs/Architecture/ADR-001-haystack-runtime.md` and `.cursor/rules/haystack-runtime.mdc`.
3. If a request conflicts with those files, follow the files and say so. To change the design, edit the ADR and the rules together.
