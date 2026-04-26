# Repository Documentation Structure

All project-related guides and architectural records are located in the top-level directory: `Documentation/`.

---

### 📂 File Directory

| File/Folder | Description |
| :--- | :--- |
| **`README_SETUP.md`** | **Setup & Local Development:** Detailed instructions on environment configuration, dependency installation, and running the app locally. |
| **`README_DOCUMENTATION.md`** | **Technical Documentation:** Records of architecture decisions, underlying logic, assumptions made during development, and technical trade-offs. |
| **`production-readiness-review.md`** | **Production Readiness** A generated report consolidating all of the known issues that would block this app from true production readiness. |
| **`screenshots/`** | **Visual Assets:** A dedicated folder containing raw image files captured from the application interface. |
| **`HoneyDo_Walkthrough.pptx`** | **Project Slide Deck:** A compiled PowerPoint presentation of the screenshots for a streamlined visual walkthrough of the app. |
| **`build_walkthrough.py`** | **Automation Script:** A Python utility used to regenerate the `HoneyDo_Walkthrough.pptx` whenever new images are added to the screenshots folder. |

---

### 🛠 Maintenance Note
If you update the application UI, please replace the old images in the `Documentation/screenshots` folder and run the builder script to keep the presentation current:

```bash
python Documentation/build_walkthrough.py
```
