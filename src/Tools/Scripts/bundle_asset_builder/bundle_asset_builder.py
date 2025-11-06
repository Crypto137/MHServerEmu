import sys
import os
import csv
import json
import shutil

CATALOG_PATH = "../../../MHServerEmu.Games/Data/Game/MTXStore/Catalog.json"

INFO_TEMPLATE = "info.html"
CONTENT_DATA_TEMPLATE = "image.png"
NAMES_TABLE = "names.tsv"

INFO_OUTPUT_DIRECTORY = "bundles"
CONTENT_DATA_OUTPUT_DIRECTORY = "bundles/images"

def build_info(entry: dict, template: str, names: dict):
    file_name = os.path.basename(entry["InfoUrls"][0]["Url"])
    output_file_path = os.path.join(INFO_OUTPUT_DIRECTORY, file_name)

    item_list = ""
    for item in entry["GuidItems"]:
        item_list += f"<li>{names[str(item['ItemPrototypeRuntimeIdForClient'])]} x{item['Quantity']}</li>"

    info = template;
    info = info.replace("%TITLE%", entry["LocalizedEntries"][0]["Title"])
    info = info.replace("%ITEMS%", item_list)
    info = info.replace("%SKU_ID%", f"0x{entry['SkuId']:X}")
    info = info.replace("%PRICE%", str(entry["LocalizedEntries"][0]["ItemPrice"]))

    os.makedirs(INFO_OUTPUT_DIRECTORY, exist_ok=True)
    with open(output_file_path, 'w') as file:
        file.write(info)

    print(file_name)

def build_image(entry: dict):
    file_name = os.path.basename(entry["ContentData"][0]["Url"])
    output_file_path = os.path.join(CONTENT_DATA_OUTPUT_DIRECTORY, file_name)
    os.makedirs(CONTENT_DATA_OUTPUT_DIRECTORY, exist_ok=True)
    shutil.copy(CONTENT_DATA_TEMPLATE, output_file_path)
    print(file_name)

def main(args: list[str]):
    info_template = ""
    with open(INFO_TEMPLATE) as file:
        info_template = file.read()

    names = {}
    with open(NAMES_TABLE) as file:
        for row in csv.reader(file, delimiter='\t'):
            names[row[0]] = row[1]

    with open(CATALOG_PATH) as file:
        catalog_data = json.load(file)
        for entry in catalog_data:
            if len(entry["InfoUrls"]) > 0:
                build_info(entry, info_template, names)

            if len(entry["ContentData"]) > 0:
                build_image(entry)
    return

if (__name__ == "__main__"):
    main(sys.argv)