#!/usr/bin/env python3
"""Setup script for FMS Log Nexus Python SDK."""

from setuptools import setup, find_packages
import os

# Read README for long description
here = os.path.abspath(os.path.dirname(__file__))
with open(os.path.join(here, "README.md"), encoding="utf-8") as f:
    long_description = f.read()

# Read version from package
version = "1.0.0"

setup(
    name="fms-lognexus",
    version=version,
    author="FMS Log Nexus Team",
    author_email="team@example.com",
    description="Python SDK for FMS Log Nexus centralized logging system",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/your-org/fms-log-nexus",
    project_urls={
        "Bug Tracker": "https://github.com/your-org/fms-log-nexus/issues",
        "Documentation": "https://github.com/your-org/fms-log-nexus/docs",
        "Source Code": "https://github.com/your-org/fms-log-nexus",
    },
    packages=find_packages(exclude=["tests", "tests.*"]),
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Topic :: System :: Logging",
        "Topic :: System :: Monitoring",
    ],
    python_requires=">=3.9",
    install_requires=[
        "requests>=2.28.0",
        "python-dateutil>=2.8.0",
    ],
    extras_require={
        "dev": [
            "pytest>=7.0.0",
            "pytest-cov>=4.0.0",
            "pytest-asyncio>=0.21.0",
            "responses>=0.23.0",
            "black>=23.0.0",
            "mypy>=1.0.0",
            "types-requests>=2.28.0",
            "types-python-dateutil>=2.8.0",
        ],
        "async": [
            "aiohttp>=3.8.0",
        ],
    },
    entry_points={
        "console_scripts": [
            "fms-lognexus=fmslognexus.cli:main",
        ],
    },
    keywords="logging monitoring fms centralized-logging job-monitoring",
    license="MIT",
    zip_safe=False,
)
