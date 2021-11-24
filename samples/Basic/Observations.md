# Observations

`Id ASC`:
Stable 100ms in both forward/backward

`Created DESC` with index:
Stable 100ms in both forward/backward

`Created DESC, Id ASC` without index:
forward 600 -> backward 1600

`Created DESC, Id ASC` with index on Created:
forward 100 -> backward 500
