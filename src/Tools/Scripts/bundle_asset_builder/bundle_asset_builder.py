import sys
import os
import csv
import json
import shutil

CATALOG_PATH = "../../../MHServerEmu.Games/Data/Game/MTXStore/Catalog.json"

INFO_TEMPLATE = "info.html"
INFO_NAMES_TABLE = "names.tsv"
IMAGE_TEMPLATE = "image.png"
IMAGE_FONT = "BebasNeue-Regular.ttf"
CSS_FILE = "style.css"

INFO_OUTPUT_DIRECTORY = "bundles"
IMAGE_OUTPUT_DIRECTORY = "bundles/images"
CSS_OUTPUT_DIRECTORY = "bundles/css"

def build_info(entry: dict, template: str, names: dict):
    # Replace placeholders with catalog data
    items = ""
    for item in entry["GuidItems"]:
        # Fall back to the id itself if no name is available.
        item_id = str(item['ItemPrototypeRuntimeIdForClient'])
        item_name = names.get(item_id, item_id)

        # Do not specify quantity if it's only one thing.
        quantity = item['Quantity']
        if quantity == 1:
            items += f"<li>{item_name}</li>"
        else:
            items += f"<li>{item_name} x{quantity}</li>"

    info = template.format(
        TITLE=entry["LocalizedEntries"][0]["Title"],
        ITEMS=items,
        SKU_ID=f"0x{entry['SkuId']:X}",
        PRICE=str(entry["LocalizedEntries"][0]["ItemPrice"])
    )

    file_name = os.path.basename(entry["InfoUrls"][0]["Url"])
    file_path = os.path.join(INFO_OUTPUT_DIRECTORY, file_name)
    os.makedirs(INFO_OUTPUT_DIRECTORY, exist_ok=True)

    with open(file_path, 'w') as file:
        file.write(info)

    print(file_name)

def build_image(entry: dict):
    file_name = os.path.basename(entry["ContentData"][0]["Url"])

    os.makedirs(IMAGE_OUTPUT_DIRECTORY, exist_ok=True)
    file_path = os.path.join(IMAGE_OUTPUT_DIRECTORY, file_name)
    shutil.copy(IMAGE_TEMPLATE, file_path)

    print(file_name)

def build_css():
    # No dynamic building happening here, just copy the premade style sheet.
    os.makedirs(CSS_OUTPUT_DIRECTORY, exist_ok=True)
    file_path = os.path.join(CSS_OUTPUT_DIRECTORY, CSS_FILE)
    shutil.copy(CSS_FILE, file_path)

def main(args: list[str]):
    info_template = ""
    with open(INFO_TEMPLATE) as file:
        info_template = file.read()

    names = {}
    with open(INFO_NAMES_TABLE) as file:
        for row in csv.reader(file, delimiter='\t'):
            names[row[0]] = row[1]

    with open(CATALOG_PATH) as file:
        catalog_data = json.load(file)
        for entry in catalog_data:
            if len(entry["InfoUrls"]) > 0:
                build_info(entry, info_template, names)

            if len(entry["ContentData"]) > 0:
                build_image(entry)

    build_css()

if (__name__ == "__main__"):
    main(sys.argv)