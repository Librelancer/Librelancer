if (!String.prototype.matchAll) {
    String.prototype.matchAll = function (regex) {
      "use strict";
      function ensureFlag(flags, flag) {
        return flags.includes(flag) ? flags : flags + flag;
      }
      function* matchAll(str, regex) {
        const localCopy = new RegExp(regex, ensureFlag(regex.flags, "g"));
        let match;
        while ((match = localCopy.exec(str))) {
          match.index = localCopy.lastIndex - match[0].length;
          yield match;
        }
      }
      return matchAll(this, regex);
    };
  }
  
  let searchIndex;
  const MAX_SUMMARY_LENGTH = 100;
  const SENTENCE_BOUNDARY_REGEX = /\b\.\s/gm;
  const WORD_REGEX = /\b(\w*)[\W|\s|\b]?/gm;
  
  function searchBoxFocused() {
    document.querySelector(".search-container").classList.add("focused");
    document
      .getElementById("search")
      .addEventListener("focusout", () => searchBoxFocusOut());
  }
  
  function searchBoxFocusOut() {
    document.querySelector(".search-container").classList.remove("focused");
  }
  document.addEventListener("DOMContentLoaded", function () {
    searchIndex = lunr(function () {
      this.field("content");
      this.ref("href");
      pagesIndex.forEach((page) => {
        this.add(page);
      });
    });
    if (document.getElementById("search-form") != null) {
      const searchInput = document.getElementById("search");
      searchInput.addEventListener("focus", () => searchBoxFocused());
      searchInput.addEventListener("keydown", (event) => {
        if (event.keyCode == 13) handleSearchQuery(event);
      });
      document
        .querySelector(".search-error")
        .addEventListener("animationend", removeAnimation);
      document
        .querySelector(".fa-search")
        .addEventListener("click", (event) => handleSearchQuery(event));
    }
  });
  
  function handleSearchQuery(event) {
    event.preventDefault();
    const query = document.getElementById("search").value.trim().toLowerCase();
    if (!query) {
      displayErrorMessage("Please enter a search term");
      return;
    }
    const results = searchSite(query);
    if (!results.length) {
      displayErrorMessage("Your search returned no results");
      return;
    }
    renderSearchResults(query, results);
  }
  
  function displayErrorMessage(message) {
    document.querySelector(".search-error-message").innerHTML = message;
    document.querySelector(".search-container").classList.remove("focused");
    document.querySelector(".search-error").classList.remove("hide-element");
    document.querySelector(".search-error").classList.add("fade");
  }
  
  function removeAnimation() {
    this.classList.remove("fade");
    this.classList.add("hide-element");
    document.querySelector(".search-container").classList.add("focused");
  }
  
  function searchSite(query) {
    const originalQuery = query;
    query = getLunrSearchQuery(query);
    let results = getSearchResults(query);
    return results.length
      ? results
      : query !== originalQuery
        ? getSearchResults(originalQuery)
        : [];
  }
  
  function getSearchResults(query) {
    return searchIndex.search(query).flatMap((hit) => {
      if (hit.ref == "undefined") return [];
      let pageMatch = pagesIndex.filter((page) => page.href === hit.ref)[0];
      pageMatch.score = hit.score;
      return [pageMatch];
    });
  }
  
  function getLunrSearchQuery(query) {
    const searchTerms = query.split(" ");
    if (searchTerms.length === 1) {
      return query;
    }
    query = "";
    for (const term of searchTerms) {
      query += `+${term} `;
    }
    return query.trim();
  }
  
  function renderSearchResults(query, results) {
    clearSearchResults();
    updateSearchResults(query, results);
    showSearchResults();
    scrollToTop();
  }
  
  function clearSearchResults() {
    const results = document.querySelector(".search-results ul");
    while (results.firstChild) results.removeChild(results.firstChild);
  }
  
  function updateSearchResults(query, results) {
    document.getElementById("query").innerHTML = query;
    document.querySelector(".search-results ul").innerHTML = results
      .map(
        (hit) => `
                    <li class="search-result-item" data-score="${hit.score.toFixed(2)}">
                      <a href="${hit.href}" class="search-result-page-title">${hit.title}</a>
                      <p>${createSearchResultBlurb(query, hit.content)}</p>
                    </li>
                    `,
      )
      .join("");
    const searchResultListItems = document.querySelectorAll(
      ".search-results ul li",
    );
    document.getElementById("results-count").innerHTML =
      searchResultListItems.length;
    document.getElementById("results-count-text").innerHTML =
      searchResultListItems.length > 1 ? "results" : "result";
    searchResultListItems.forEach(
      (li) =>
        (li.firstElementChild.style.color = getColorForSearchResult(
          li.dataset.score,
        )),
    );
  }
  
  function createSearchResultBlurb(query, pageContent) {
    const searchQueryRegex = new RegExp(createQueryStringRegex(query), "gmi");
    const searchQueryHits = Array.from(
      pageContent.matchAll(searchQueryRegex),
      (m) => m.index,
    );
    const sentenceBoundaries = Array.from(
      pageContent.matchAll(SENTENCE_BOUNDARY_REGEX),
      (m) => m.index,
    );
    let searchResultText = "";
    let lastEndOfSentence = 0;
    for (const hitLocation of searchQueryHits) {
      if (hitLocation > lastEndOfSentence) {
        for (let i = 0; i < sentenceBoundaries.length; i++) {
          if (sentenceBoundaries[i] > hitLocation) {
            const startOfSentence = i > 0 ? sentenceBoundaries[i - 1] + 1 : 0;
            const endOfSentence = sentenceBoundaries[i];
            lastEndOfSentence = endOfSentence;
            parsedSentence = pageContent
              .slice(startOfSentence, endOfSentence)
              .trim();
            searchResultText += `${parsedSentence} ... `;
            break;
          }
        }
      }
      const searchResultWords = tokenize(searchResultText);
      const pageBreakers = searchResultWords.filter((word) => word.length > 50);
      if (pageBreakers.length > 0) {
        searchResultText = fixPageBreakers(searchResultText, pageBreakers);
      }
      if (searchResultWords.length >= MAX_SUMMARY_LENGTH) break;
    }
    return ellipsize(searchResultText, MAX_SUMMARY_LENGTH).replace(
      searchQueryRegex,
      "<strong>$&</strong>",
    );
  }
  
  function createQueryStringRegex(query) {
    const searchTerms = query.split(" ");
    if (searchTerms.length == 1) {
      return query;
    }
    query = "";
    for (const term of searchTerms) {
      query += `${term}|`;
    }
    query = query.slice(0, -1);
    return `(${query})`;
  }
  
  function tokenize(input) {
    const wordMatches = Array.from(input.matchAll(WORD_REGEX), (m) => m);
    return wordMatches.map((m) => ({
      word: m[0],
      start: m.index,
      end: m.index + m[0].length,
      length: m[0].length,
    }));
  }
  
  function fixPageBreakers(input, largeWords) {
    largeWords.forEach((word) => {
      const chunked = chunkify(word.word, 20);
      input = input.replace(word.word, chunked);
    });
    return input;
  }
  
  function chunkify(input, chunkSize) {
    let output = "";
    let totalChunks = (input.length / chunkSize) | 0;
    let lastChunkIsUneven = input.length % chunkSize > 0;
    if (lastChunkIsUneven) {
      totalChunks += 1;
    }
    for (let i = 0; i < totalChunks; i++) {
      let start = i * chunkSize;
      let end = start + chunkSize;
      if (lastChunkIsUneven && i === totalChunks - 1) {
        end = input.length;
      }
      output += input.slice(start, end) + " ";
    }
    return output;
  }
  
  function ellipsize(input, maxLength) {
    const words = tokenize(input);
    if (words.length <= maxLength) {
      return input;
    }
    return input.slice(0, words[maxLength].end) + "...";
  }
  
  function showSearchResults() {
    document.querySelector(".search-results").classList.remove("hide-element");
    document.getElementById("site-search").classList.add("expanded");
  }
  
  function scrollToTop() {
    const toTopInterval = setInterval(function () {
      const supportedScrollTop =
        document.body.scrollTop > 0 ? document.body : document.documentElement;
      if (supportedScrollTop.scrollTop > 0) {
        supportedScrollTop.scrollTop = supportedScrollTop.scrollTop - 50;
      }
      if (supportedScrollTop.scrollTop < 1) {
        clearInterval(toTopInterval);
      }
    }, 10);
  }
  
  function getColorForSearchResult(score) {
    const warmColorHue = 171;
    const coolColorHue = 212;
    return adjustHue(warmColorHue, coolColorHue, score);
  }
  
  function adjustHue(hue1, hue2, score) {
    if (score > 3) return `hsl(${hue1}, 100%, 50%)`;
    const hueAdjust = (parseFloat(score) / 3) * (hue1 - hue2);
    const newHue = hue2 + Math.floor(hueAdjust);
    return `hsl(${newHue}, 100%, 50%)`;
  }
  