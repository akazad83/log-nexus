#!/usr/bin/env python3
"""Setup script for FMS Log Nexus CLI."""

from setuptools import setup, find_packages

setup(
    name="fms-lognexus-cli",
    version="1.0.0",
    author="FMS Log Nexus Team",
    author_email="team@example.com",
    description="Command-line administration tool for FMS Log Nexus",
    long_description=open("README.md").read(),
    long_description_content_type="text/markdown",
    url="https://github.com/your-org/fms-log-nexus",
    py_modules=["fms_cli"],
    install_requires=[
        "click>=8.0.0",
        "requests>=2.28.0",
        "fms-lognexus>=1.0.0",
    ],
    entry_points={
        "console_scripts": [
            "fms-cli=fms_cli:main",
        ],
    },
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Environment :: Console",
        "Intended Audience :: System Administrators",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Topic :: System :: Logging",
        "Topic :: System :: Systems Administration",
    ],
    python_requires=">=3.9",
    keywords="logging monitoring fms cli administration",
    license="MIT",
)
