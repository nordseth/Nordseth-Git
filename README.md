# Nordseth-Git

[![NuGet (Nordseth.Git)](https://img.shields.io/nuget/v/Nordseth.Git)](https://www.nuget.org/packages/Nordseth.Git/)

A small library for reading git repos.

It was written for reading commit information with a managed library for [Nibbler](https://github.com/nordseth/Nibbler),
as a replacement for [LibGit2Sharp](https://github.com/libgit2/libgit2sharp/). 
LibGit2Sharp has large native dependencies that make multiplatform CLI tools about 30 mb larger.

## Usage

``` C#
var repo = new Repo(path);
var (_, hash) = repo.GetHead();
var commit = repo.GetCommit(hash);
Console.WriteLine($"Commit at HEAD: '{commit.MessageShort}' by {commit.Author.Name} at {commit.Author.When}");
```
