# Changelog

All notable changes to this project will be documented in this file.

Make sure to always view this file from the main branch to get an up to date changelog.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## Unreleased

- Support booleans. [[#16](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/16)]

## 1.0.1 - 2022-02-18

https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/releases/tag/v1.0.1

- Fix problem with nullable props. [[#14](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/14)]

**Full Changelog**: https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v1.0.0...v1.0.1

## 1.0.0 - 2021-12-01

https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/releases/tag/v1.0.0

- Optimize the generated predicate expression to use an access predicate. [[#8](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/8)]
- Throw custom exception when data type is incompatible. [[#11](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/11)]
- Add EnsureCorrectOrder to KeysetContext.

**Full Changelog**: https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v0.2.0...v1.0.0

## 0.2.1 - 2021-11-24

https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/releases/tag/v0.2.1

- Fixed order of expressions generated for the order by when acting on more than one column.

**Full Changelog**: https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v0.2.0...v0.2.1

## 0.2.0 - 2021-11-23

https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/releases/tag/v0.2.0

- Update to .net 6.
- Use nullable references types.

**Full Changelog**: https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/compare/v0.1.3...v0.2.0

## 0.1.3 - 2021-11-01

### Fixed

- Support Guids. [#1](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/issues/1)
