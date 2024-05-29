# Contributing to Epam.Kafka

First off, thanks for taking the time to contribute! ❤️

All types of contributions are encouraged and valued. Please refer to the [Table of Contents](#table-of-contents) for information on various ways you can contribute and details about how this project handles them. Please make sure to read the relevant section before making your contribution. It will make it a lot easier for us maintainers and smooth out the experience for all involved. 

Thank you for being a part of our community and making a difference through your contributions. 🎉

> And if you like the project, but just don't have time to contribute, that's fine. There are other easy ways to support the project and show your appreciation, which we would also be very happy about:
> - Star the project;
> - Tweet about it;
> - Refer to this project in your project's readme;
> - Mention the project at local meetups and tell your friends/colleagues.

## Table of Contents

- [I Have a Question](#i-have-a-question)
- [I Have an Issue](#i-have-an-issue)
  - [Reporting Bugs](#reporting-bugs)
- [I Have an Idea](#i-have-an-idea)
  - [Suggesting For Enhancement](#suggesting-for-enhancement)
- [I Want To Submit Changes](#i-want-to-submit-changes)
  - [Creating a Pull Request](#creating-a-pull-request)

## Legal Notice

> When contributing to this project, you must agree that you have authored 100% of the content, that you have the necessary rights to the content, and that the content you contribute may be provided under the project license.

## I Have a Question

> If you want to ask a question, we assume that you have read the available [Documentation](https://github.com/epam/epam-kafka).

Before you ask a question, it is best to search for existing [issues](https://github.com/epam/epam-kafka/issues) that might help you. In case you have found a suitable issue and still need clarification, you can write your question in this issue. It is also advisable to search the internet for answers first.

If you still feel the need to ask a question and need clarification, we recommend the following:

- Open a new [Discussion](https://github.com/epam/epam-kafka/discussions/new);
- Provide as much context as you can about what you're running into;
- Add corresponding label

We will then take care of your question as soon as possible.

## I Have an Issue

### Reporting Bugs

#### Before Submitting a Bug Report

A good bug report shouldn't leave others needing to chase you up for more information. Therefore, we ask you to investigate carefully, collect information, and describe the issue in detail in your report. Please complete the following steps in advance to help us fix any potential bug as fast as possible.

- Make sure that you are using the latest version;
- Determine if your bug is really a bug and not an error on your side e.g. using incompatible environment components/versions (Make sure that you have read the [Documentation](https://github.com/epam/epam-kafka). If you are looking for support, you might want to check [This Section](#i-have-a-question));
- To see if other users have experienced (and potentially already solved) the same issue you are having, check if there is not already a bug report existing for your error in the [issues](https://github.com/epam/epam-kafka/issues?q=is%3Aopen+is%3Aissue+label%3Abug).

#### How Do I Submit a Good Bug Report?

We use [GitHub Issues](https://github.com/epam/epam-kafka/issues) to track bugs and errors. If you run into an issue with the project, please open a new [bug report](https://github.com/epam/epam-kafka/issues/new?assignees=&labels=bug&projects=&template=bug-report.md&title=%5BComponent+name%5D%3A+clear+and+descriptive+title) and follow its template. The template provides details on all the required and optional sections, along with guidance on how to fill them out.

#### General Information About Bug Report

All defects should be created with the label "**bug**". If you use the default bug template, this label will be set automatically. 
You can add an additional label(s) both during the creation of the bug and after it has been saved.

All bugs are considered with "**minor**" priority by default and will be taken to work in order of priority. If you think your bug is higher level, add the "**medium**' or  "**major**" priority to the ticket. To set the priority follow the next steps: Open the saved task -> Find "Projects" section on the right -> Click on the dropdown on the right -> Click on the dropdown with priorities and choose the one that suits you.

## I Have an Idea

### Suggesting For Enhancement

This section guides you through submitting an enhancement suggestion for Epam.Kafka, including completely new features and enhancements to existing functionality. Following these guidelines will help maintainers and the community to understand your suggestion and identify related recommendations.

#### Before Submitting an Enhancement

- Make sure that you are using the latest version;
- Read the [Documentation](https://github.com/epam/epam-kafka) carefully and find out if the functionality is already covered, maybe by an individual configuration;
- Perform a [search](https://github.com/epam/epam-kafka/issues?q=is%3Aopen+is%3Aissue+label%3Aenhancement) to see if the enhancement has already been suggested. If it has, add a comment to the existing issue instead of opening a new one;
- Find out whether your idea fits with the scope and aims of the project. It's up to you to make a strong case to convince the project's developers of the merits of this feature. Keep in mind that we want features that will be useful to the majority of our users and not just a small subset. If you're just targeting a minority of users, consider finding some workaround or try to implement it in your own code if it possible.

#### How Do I Submit a Good Enhancement?

Enhancement is tracked as [GitHub Issues](https://github.com/epam/epam-kafka/issues). You can create a new one using [enhancement template](https://github.com/epam/epam-kafka/issues/new?assignees=&labels=enhancement&projects=&template=enhancement.md&title=).

- Use a clear and descriptive title for the issue to identify the suggestion;
- Describe the current behavior and explain which behavior you expected to see instead and why. At this point you can also tell which alternatives do not work for you;
- Explain why this enhancement would be useful to most Epam.Kafka users. You may also want to point out the other projects that solved it better and which could serve as inspiration.

## I Want To Submit Changes

### Creating a Pull Request

> Before you will make your pull request, please first discuss the change you wish to make via bug report, feature request or discussion.

You can contribute to our codebase via creating Pull Request. PRs for our libraries are always welcome and can be a quick way to get your fix or enhancement planned for the next release. 

In general, we follow the ["Fork-and-Pull" Git workflow](https://github.com/susam/gitpr)

1. Fork and clone the repository and create your branch from the `develop` branch;
2. Create a branch locally with a short but descriptive name;
3. If you've fixed a bug or added code that should be tested, add tests;
4. Ensure the test suite passes (build.cmd);
5. If you make api changes or add new functionality, add example to the documentation;
6. Commit and push to your fork;
7. Open a PR in our repository to `develop` branch.
